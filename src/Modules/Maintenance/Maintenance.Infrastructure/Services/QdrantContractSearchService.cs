using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Maintenance.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Maintenance.Infrastructure.Services;

/// <summary>
/// Searches the Qdrant 'contracts' collection for lease contract clauses
/// relevant to a given maintenance issue description.
/// </summary>
public sealed class QdrantContractSearchService : IContractClauseSearchService
{
    private const string Collection = "contracts";

    private readonly QdrantClient _qdrant;
    private readonly HttpClient _http;
    private readonly string _geminiApiKey;
    private readonly string _geminiModel;
    private readonly ILogger<QdrantContractSearchService> _logger;

    public QdrantContractSearchService(
        IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<QdrantContractSearchService> logger)
    {
        _logger = logger;

        // Qdrant cloud (sa-east-1, gRPC over TLS port 6334)
        var host    = config["Qdrant:Endpoint"]!;
        var apiKey  = config["Qdrant:ApiKey"]!;
        _qdrant = new QdrantClient(host: host, https: true, apiKey: apiKey, port: 6334);

        // Gemini embeddings (text-embedding-004) — replaces Azure OpenAI ada-002
        _http = httpClientFactory.CreateClient(nameof(QdrantContractSearchService));
        _geminiApiKey = config["AI:GeminiApiKey"] ?? string.Empty;
        _geminiModel = config["AI:GeminiEmbeddingModel"] ?? "text-embedding-004";
    }

    private async Task<float[]> EmbedAsync(string text, CancellationToken ct)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_geminiModel}:embedContent?key={_geminiApiKey}";
        var request = new EmbedRequest
        {
            Model = $"models/{_geminiModel}",
            Content = new GeminiContent { Parts = [new GeminiPart { Text = text }] }
        };

        using var response = await _http.PostAsJsonAsync(url, request, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<EmbedResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Gemini embeddings API returned an empty response.");

        return result.Embedding.Values.ToArray();
    }

    private sealed class EmbedRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public GeminiContent Content { get; set; } = new();
    }

    private sealed class GeminiContent
    {
        [JsonPropertyName("parts")]
        public List<GeminiPart> Parts { get; set; } = [];
    }

    private sealed class GeminiPart
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    private sealed class EmbedResponse
    {
        [JsonPropertyName("embedding")]
        public GeminiEmbedding Embedding { get; set; } = new();
    }

    private sealed class GeminiEmbedding
    {
        [JsonPropertyName("values")]
        public List<float> Values { get; set; } = [];
    }

    public async Task<IReadOnlyList<string>> SearchAsync(
        string leaseId,
        string issueDescription,
        int topK = 5,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(leaseId))
        {
            _logger.LogWarning("SearchAsync called with empty leaseId — returning no clauses.");
            return [];
        }

        try
        {
            // 1. Embed a maintenance-focused query so the vector matches contract obligation clauses
            var embeddingQuery =
                $"Who is responsible for maintenance and repair obligations regarding: {issueDescription}";
            var vector = await EmbedAsync(embeddingQuery, ct);

            // 2. Filter by contractId = leaseId (indexed as keyword in Qdrant)
            var filter = new Filter();
            filter.Must.Add(new Condition
            {
                Field = new FieldCondition
                {
                    Key   = "contractId",
                    Match = new Match { Keyword = leaseId }
                }
            });

            // 3. Semantic search
            var hits = await _qdrant.SearchAsync(
                collectionName:  Collection,
                vector:          vector,
                filter:          filter,
                limit:           (ulong)topK,
                payloadSelector: new WithPayloadSelector { Enable = true },
                cancellationToken: ct);

            _logger.LogInformation(
                "Qdrant returned {Count} clause(s) for lease {LeaseId}", hits.Count, leaseId);

            if (hits.Count > 0)
                _logger.LogInformation(
                    "First hit payload keys: [{Keys}]",
                    string.Join(", ", hits[0].Payload.Keys));

            // 4. Extract text — try common field names used by n8n / LangChain / Python
            var fieldCandidates = new[]
            {
                "pageContent",   // n8n LangChain node default
                "page_content",  // Python LangChain default
                "content",
                "text",
                "chunk",
                "chunk_content",
                "document"
            };

            return hits
                .Select(h =>
                {
                    foreach (var field in fieldCandidates)
                    {
                        if (h.Payload.TryGetValue(field, out var val) &&
                            !string.IsNullOrWhiteSpace(val.StringValue))
                            return val.StringValue;
                    }

                    _logger.LogWarning(
                        "Hit {Id}: none of the expected payload fields found. Available: [{Keys}]",
                        h.Id, string.Join(", ", h.Payload.Keys));
                    return null;
                })
                .Where(s => s is not null)
                .Cast<string>()
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Qdrant search failed for lease {LeaseId}", leaseId);
            return [];
        }
    }
}
