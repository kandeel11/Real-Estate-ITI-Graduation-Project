// AI.Infrastructure/Services/PropertyDuplicateCheckService.cs

using AI.Application.DTOs;
using AI.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AI.Infrastructure.Services;

/// <summary>
/// Duplicate property detection using:
///   - OpenAI text-embedding-3-small for vector generation
///   - Qdrant cosine similarity for fast approximate search with payload filtering
///   - Rule Engine for deterministic checks (Cost saving)
///   - Claude for intelligent duplicate confirmation (Ambiguous cases)
/// </summary>
public sealed class PropertyDuplicateCheckService : IPropertyDuplicateCheckService
{
    private readonly ITextEmbeddingGenerationService _embeddings;
    private readonly IChatCompletionService _chat;
    private readonly Kernel _kernel;
    private readonly QdrantClient _qdrant;
    private readonly ILogger<PropertyDuplicateCheckService> _logger;

    // ── Configuration ─────────────────────────────────────────────────────────
    private const string CollectionName = "properties";
    private const int EmbeddingDimension = 1536;

    // Adjusted thresholds based on best practices for embeddings
    private const double SimilarityThreshold = 0.82;   // Min score to fetch from Qdrant
    private const double AutoDuplicateThreshold = 0.97; // Score high enough to skip Claude
    private const double ClaudeReviewThreshold = 0.85;  // Score below this skips Claude

    private const int TopKSearch = 5;

    public PropertyDuplicateCheckService(
        ITextEmbeddingGenerationService embeddings,
        IChatCompletionService chat,
        Kernel kernel,
        QdrantClient qdrant,
        ILogger<PropertyDuplicateCheckService> logger)
    {
        _embeddings = embeddings;
        _chat = chat;
        _kernel = kernel;
        _qdrant = qdrant;
        _logger = logger;
    }

    public async Task<DuplicateCheckResult> CheckAsync(
        PropertyDuplicateCheckRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Duplicate check started | PropertyId={Id} | {Title} | {City} {District}",
            request.PropertyId, request.Title, request.City, request.District);

        // ── Step 1: Build structured searchable text ──────────────────────────
        var searchText = BuildSearchText(request);
        _logger.LogDebug("Search text: \n{Text}", searchText);

        // ── Step 2: Generate embedding ────────────────────────────────────────
        var embeddings = await _embeddings.GenerateEmbeddingsAsync(
            [searchText], cancellationToken: cancellationToken);
        var vector = embeddings[0].ToArray();

        // ── Step 3: Ensure Qdrant collection safely ───────────────────────────
        await EnsureCollectionAsync(cancellationToken);

        // ── Step 4: Search Qdrant with Filters ────────────────────────────────
        var searchFilter = BuildSearchFilter(request);

        var hits = await _qdrant.SearchAsync(
            collectionName: CollectionName,
            vector: vector,
            filter: searchFilter, // Applied Filter to narrow down search
            limit: TopKSearch,
            scoreThreshold: (float)SimilarityThreshold,
            cancellationToken: cancellationToken);

        var similarHits = hits
    .Where(h =>
        h.Payload.TryGetValue("propertyId", out var propertyId)
        && propertyId.StringValue != request.PropertyId.ToString())
    .OrderByDescending(h => h.Score)
    .ToList();

        _logger.LogInformation(
            "Qdrant search returned {Count} hits above threshold {T}",
            similarHits.Count, SimilarityThreshold);

        // ── Step 5: Rule Engine (Cost Saving & Fast Evaluation) ───────────────
        DuplicateCheckResult finalResult;

        if (similarHits.Count == 0)
        {
            // Rule 1: No hits found at all -> Not a duplicate
            finalResult = CreateSafeResult(false, 0, similarHits, request,
                "No similar properties found.", "Clean property.");
        }
        else
        {
            var highestScore = similarHits[0].Score;
            var isSameOwner = string.Equals(similarHits[0].Payload["ownerId"].StringValue, request.OwnerId.ToString(), StringComparison.OrdinalIgnoreCase);

            if (highestScore >= AutoDuplicateThreshold || (highestScore >= 0.90 && isSameOwner))
            {
                // Rule 2: Extremely high similarity or high similarity with same owner -> Auto Duplicate
                finalResult = CreateSafeResult(true, highestScore, similarHits, request,
                    "Exact match or very high similarity with same owner detected.", "Auto-flagged by Rule Engine.");
            }
            else if (highestScore < ClaudeReviewThreshold)
            {
                // Rule 3: Below Claude review threshold -> Not a duplicate
                finalResult = CreateSafeResult(false, highestScore, similarHits, request,
                    "Similarity score below ambiguity threshold.", "Auto-cleared by Rule Engine.");
            }
            else
            {
                // Rule 4: Ambiguous case -> Ask Claude
                finalResult = await AnalyzeWithClaudeAsync(request, similarHits, cancellationToken);
            }
        }

        // ── Step 6: Upsert ONLY IF NOT duplicate ──────────────────────────────
        if (!finalResult.IsDuplicate)
        {
            await UpsertPropertyAsync(request, vector, cancellationToken);
        }
        else
        {
            _logger.LogInformation(
                "Property {Id} is a duplicate. Skipping database insertion.", request.PropertyId);
        }

        _logger.LogInformation(
            "Duplicate check done | PropertyId={Id} | IsDuplicate={Dup} | Score={Score}",
            request.PropertyId, finalResult.IsDuplicate, finalResult.HighestSimilarityScore);

        return finalResult;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Build Structured Search Text
    // ─────────────────────────────────────────────────────────────────────────
    private static string BuildSearchText(PropertyDuplicateCheckRequest request)
    {
        // Normalize: if city == district (New Cairo case), use district only
        var location = string.Equals(request.City, request.District, StringComparison.OrdinalIgnoreCase)
            ? request.District
            : $"{request.District}, {request.City}";

        return $"""
        {request.PropertyType} {request.ListingType} in {location}
        Bedrooms: {request.Bedrooms} | Bathrooms: {request.Bathrooms}
        Floor: {request.Floor} | Area: {request.AreaInMeters}sqm
        Price: {request.Price}
        Street: {request.Street} | Unit: {request.UnitNumber}
        Building: {request.BuildingName}
        Title: {request.Title}
        """;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Build Qdrant Search Filter
    // ─────────────────────────────────────────────────────────────────────────
    private Filter BuildSearchFilter(PropertyDuplicateCheckRequest request)
    {
        var conditions = new List<Condition>();

        if (!string.IsNullOrWhiteSpace(request.District))
        {
            conditions.Add(new Condition
            {
                Field = new FieldCondition
                {
                    Key = "district",
                    Match = new Match
                    {
                        Keyword = request.District
                    }
                }
            });
        }

        if (!string.IsNullOrWhiteSpace(request.PropertyType))
        {
            conditions.Add(new Condition
            {
                Field = new FieldCondition
                {
                    Key = "propertyType",
                    Match = new Match
                    {
                        Keyword = request.PropertyType
                    }
                }
            });
        }

        if (!string.IsNullOrWhiteSpace(request.ListingType))
        {
            conditions.Add(new Condition
            {
                Field = new FieldCondition
                {
                    Key = "listingType",
                    Match = new Match
                    {
                        Keyword = request.ListingType
                    }
                }
            });
        }

        return conditions.Count > 0
            ? new Filter { Must = { conditions } }
            : new Filter();
    }
    //────────────────────────────────────────────────────────────────────
    // Upsert property into Qdrant
    // ─────────────────────────────────────────────────────────────────────────
    private async Task UpsertPropertyAsync(
        PropertyDuplicateCheckRequest request,
        float[] vector,
        CancellationToken ct)
    {
        var point = new PointStruct
        {
            Id = new PointId { Uuid = request.PropertyId.ToString() },
            Vectors = new Vectors { Vector = new Vector { Data = { vector } } },
            Payload =
            {
                ["propertyId"]   = request.PropertyId.ToString(),
                ["ownerId"]      = request.OwnerId.ToString(),
                ["title"]        = request.Title,
                ["city"]         = request.City,
                ["district"]     = request.District,
                ["street"]       = request.Street,
                ["buildingName"] = request.BuildingName ?? string.Empty,
                ["unitNumber"]   = request.UnitNumber   ?? string.Empty,
                ["floor"]        = request.Floor        ?? 0,
                ["propertyType"] = request.PropertyType,
                ["listingType"]  = request.ListingType,
                ["area"]         = request.AreaInMeters,
                ["bedrooms"]     = request.Bedrooms  ?? 0,
                ["bathrooms"]    = request.Bathrooms ?? 0,
                ["price"]        = (double)request.Price
            }
        };

        await _qdrant.UpsertAsync(CollectionName, [point], cancellationToken: ct);
        _logger.LogInformation("Property {Id} upserted into Qdrant '{Collection}'", request.PropertyId, CollectionName);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Claude analyzes hits for ambiguous cases
    // ─────────────────────────────────────────────────────────────────────────
    private async Task<DuplicateCheckResult> AnalyzeWithClaudeAsync(
        PropertyDuplicateCheckRequest request,
        IReadOnlyList<ScoredPoint> hits,
        CancellationToken ct)
    {
        var similarProperties = hits.Select(h => new SimilarProperty(
            PropertyId: Guid.Parse(h.Payload["propertyId"].StringValue),
            OwnerId: Guid.Parse(h.Payload["ownerId"].StringValue),
            Title: h.Payload["title"].StringValue,
            City: h.Payload["city"].StringValue,
            District: h.Payload["district"].StringValue,
            Street: h.Payload["street"].StringValue,
            Score: h.Score,
            MatchedFields: BuildMatchedFields(request, h)
        )).ToList();

        var similarSummary = new StringBuilder();
        foreach (var hit in hits)
        {
            similarSummary.AppendLine($$"""
    ── Similar Property (Similarity: {{hit.Score:P1}}) ──
    Property ID  : {{hit.Payload["propertyId"].StringValue}}
    Owner ID     : {{hit.Payload["ownerId"].StringValue}}
    Title        : {{hit.Payload["title"].StringValue}}
    Listing Type : {{hit.Payload["listingType"].StringValue}}
    Type         : {{hit.Payload["propertyType"].StringValue}}
    City         : {{hit.Payload["city"].StringValue}}
    District     : {{hit.Payload["district"].StringValue}}
    Street       : {{hit.Payload["street"].StringValue}}
    Building     : {{hit.Payload["buildingName"].StringValue}}
    Unit Number  : {{hit.Payload["unitNumber"].StringValue}}
    Floor        : {{hit.Payload["floor"].IntegerValue}}
    Area         : {{hit.Payload["area"].DoubleValue}} m²
    Bedrooms     : {{hit.Payload["bedrooms"].IntegerValue}}
    Bathrooms    : {{hit.Payload["bathrooms"].IntegerValue}}
    Price        : {{hit.Payload["price"].DoubleValue}}
    """);
        }

        var prompt = $$"""
            You are a strict real estate duplicate detection specialist.
            Determine whether the NEW PROPERTY is a duplicate of any CANDIDATE below.
 
            ═══ NEW PROPERTY ═══
            Owner ID     : {{request.OwnerId}}
            Title        : {{request.Title}}
            Listing Type : {{request.ListingType}}
            Type         : {{request.PropertyType}}
            City         : {{request.City}}
            District     : {{request.District}}
            Street       : {{request.Street}}
            Building     : {{request.BuildingName ?? "N/A"}}
            Unit Number  : {{request.UnitNumber ?? "N/A"}}
            Floor        : {{request.Floor?.ToString() ?? "N/A"}}
            Area         : {{request.AreaInMeters}} m²
            Bedrooms     : {{request.Bedrooms ?? 0}}
            Bathrooms    : {{request.Bathrooms ?? 0}}
            Price        : {{request.Price}}
 
            ═══ CANDIDATES ═══
            {{similarSummary}}
 
            ═══ STRICT DUPLICATE RULES ═══
            Mark is_duplicate = true when MOST strong signals indicate
            that both listings refer to the same physical property.

            Strong signals:
            ✓ Same district AND same street
            ✓ Same building name (if available)
            ✓ Same unit number OR same floor
            ✓ Same property type
            ✓ Area within 10%
            ✓ Price within 25%
            ✓ Same owner increases confidence

            A few missing fields (building/unit/floor) should NOT automatically
            mean false if the remaining evidence is strong.

            Mark is_duplicate = false when there is strong contradictory evidence:
            ✗ Different street
            ✗ Different unit number in same building
            ✗ Different property type
            ✗ Area difference > 20%
            ✗ Listing type mismatch (ForSale vs ForRent)
 
            ═══ SCORING GUIDANCE ═══
            Return:
            - similarity_score:
              how semantically similar the listings are (0-1)

            - decision_confidence:
              how confident you are in your final duplicate decision (0-1)
            This may differ from the Qdrant cosine score shown above.
 
            ═══ ADMIN NOTE ═══
            Write one concise sentence naming the matched fields, differing fields,
            and your final recommendation.
 
            Return ONLY valid JSON — no preamble, no markdown fences:
            {
              "is_duplicate": false,
              "similarity_score": 0.91,
              "decision_confidence": 0.62,
              "reasoning": "...",
              "admin_note": "..."
            }
            """;

        var history = new ChatHistory();
        history.AddUserMessage(prompt);

        var response = await _chat.GetChatMessageContentAsync(
            history, kernel: _kernel, cancellationToken: ct);
        _logger.LogInformation(
    "Claude raw response: {Response}",
    response.Content);

        return ParseClaudeResponse(response.Content ?? "{}", similarProperties, hits, request);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Parse Claude JSON safely via JsonSerializer
    // ─────────────────────────────────────────────────────────────────────────
    private DuplicateCheckResult ParseClaudeResponse(
        string raw,
        List<SimilarProperty> similarProperties,
        IReadOnlyList<ScoredPoint> hits,
        PropertyDuplicateCheckRequest request)
    {
        try
        {
            var json = raw.Replace("```json", "").Replace("```", "").Trim();
            var parsed = JsonSerializer.Deserialize<ClaudeDuplicateResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (parsed == null) throw new InvalidOperationException("Deserialized object is null.");

            return new DuplicateCheckResult(
     IsDuplicate: parsed.IsDuplicate,
     HighestSimilarityScore: parsed.SimilarityScore,
                 Threshold: SimilarityThreshold,
                SimilarProperties: similarProperties,
                Reasoning: parsed.Reasoning ?? string.Empty,
                AdminNote: parsed.AdminNote ?? string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Claude response: {Raw}", raw);

            // Fallback to Rule Engine logic if LLM fails
            var highestScore = hits.Count > 0 ? hits.Max(h => h.Score) : 0;
            return CreateSafeResult(highestScore >= 0.90, highestScore, hits.ToList(), request,
                "Analysis based on similarity score only due to AI parsing failure.",
                "Automatic analysis failed — manual review required.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Build matched fields summary with safe checks
    // ─────────────────────────────────────────────────────────────────────────
    private static string BuildMatchedFields(PropertyDuplicateCheckRequest request, ScoredPoint hit)
    {
        var matched = new List<string>();

        // Safe String Comparisons
        if (string.Equals(hit.Payload["city"].StringValue, request.City, StringComparison.OrdinalIgnoreCase)) matched.Add("city");
        if (string.Equals(hit.Payload["district"].StringValue, request.District, StringComparison.OrdinalIgnoreCase)) matched.Add("district");
        if (string.Equals(hit.Payload["street"].StringValue, request.Street, StringComparison.OrdinalIgnoreCase)) matched.Add("street");

        if (!string.IsNullOrEmpty(request.BuildingName) &&
            string.Equals(hit.Payload["buildingName"].StringValue, request.BuildingName, StringComparison.OrdinalIgnoreCase))
            matched.Add("building");

        if (!string.IsNullOrEmpty(request.UnitNumber) &&
            string.Equals(hit.Payload["unitNumber"].StringValue, request.UnitNumber, StringComparison.OrdinalIgnoreCase))
            matched.Add("unit#");

        if (request.Floor.HasValue && hit.Payload["floor"].IntegerValue == request.Floor.Value)
            matched.Add("floor");

        if (string.Equals(hit.Payload["propertyType"].StringValue, request.PropertyType, StringComparison.OrdinalIgnoreCase))
            matched.Add("type");

        // Safe Math (Division by zero protection)
        if (request.AreaInMeters > 0)
        {
            var areaDiff = Math.Abs(hit.Payload["area"].DoubleValue - request.AreaInMeters);
            if (areaDiff / request.AreaInMeters < 0.10) matched.Add("area");
        }

        if (request.Bedrooms.HasValue && hit.Payload["bedrooms"].IntegerValue == request.Bedrooms.Value)
            matched.Add("bedrooms");

        if (request.Price > 0)
        {
            var priceDiff = Math.Abs(hit.Payload["price"].DoubleValue - (double)request.Price);
            if (priceDiff / (double)request.Price < 0.20) matched.Add("price");
        }

        return matched.Count > 0 ? string.Join(", ", matched) : "low textual similarity";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Qdrant collection setup with Race Condition Protection
    // ─────────────────────────────────────────────────────────────────────────
    private async Task EnsureCollectionAsync(CancellationToken ct)
    {
        try
        {
            if (await _qdrant.CollectionExistsAsync(CollectionName, ct)) return;

            _logger.LogInformation("Creating Qdrant collection '{Collection}'", CollectionName);

            await _qdrant.CreateCollectionAsync(CollectionName,
                new VectorParams
                {
                    Size = EmbeddingDimension,
                    Distance = Distance.Cosine
                }, cancellationToken: ct);

            await _qdrant.CreatePayloadIndexAsync(CollectionName, "city", PayloadSchemaType.Keyword, cancellationToken: ct);
            await _qdrant.CreatePayloadIndexAsync(CollectionName, "district", PayloadSchemaType.Keyword, cancellationToken: ct);
            await _qdrant.CreatePayloadIndexAsync(CollectionName, "propertyType", PayloadSchemaType.Keyword, cancellationToken: ct);
            await _qdrant.CreatePayloadIndexAsync(CollectionName, "listingType", PayloadSchemaType.Keyword, cancellationToken: ct);
            await _qdrant.CreatePayloadIndexAsync(CollectionName, "ownerId", PayloadSchemaType.Keyword, cancellationToken: ct);

            _logger.LogInformation("Qdrant collection and indexes ready");
        }
        catch (Exception ex)
        {
            // Catches race conditions where another thread/instance created it milliseconds ago
            _logger.LogWarning(ex, "Collection creation threw an exception. It might have been created concurrently.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helper to generate safe Result structs
    // ─────────────────────────────────────────────────────────────────────────
    private DuplicateCheckResult CreateSafeResult(
        bool isDuplicate, double highestScore, List<ScoredPoint> hits, PropertyDuplicateCheckRequest req, string reason, string note)
    {
        var mappedHits = hits.Select(h => new SimilarProperty(
            PropertyId: Guid.Parse(h.Payload["propertyId"].StringValue),
            OwnerId: Guid.Parse(h.Payload["ownerId"].StringValue),
            Title: h.Payload["title"].StringValue,
            City: h.Payload["city"].StringValue,
            District: h.Payload["district"].StringValue,
            Street: h.Payload["street"].StringValue,
            Score: h.Score,
            MatchedFields: BuildMatchedFields(req, h)
        )).ToList();

        return new DuplicateCheckResult(isDuplicate, highestScore, SimilarityThreshold, mappedHits, reason, note);
    }

    // Record for safe JSON Parsing
    private sealed record ClaudeDuplicateResponse(
     [property: JsonPropertyName("is_duplicate")]
    bool IsDuplicate,

     [property: JsonPropertyName("similarity_score")]
    double SimilarityScore,

     [property: JsonPropertyName("decision_confidence")]
    double DecisionConfidence,

     [property: JsonPropertyName("reasoning")]
    string Reasoning,   

     [property: JsonPropertyName("admin_note")]
    string AdminNote
 );
}