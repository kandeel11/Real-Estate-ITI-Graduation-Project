using Azure;
using Azure.AI.OpenAI;
using Maintenance.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Embeddings;
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
    private readonly EmbeddingClient _embedder;
    private readonly ILogger<QdrantContractSearchService> _logger;

    public QdrantContractSearchService(IConfiguration config, ILogger<QdrantContractSearchService> logger)
    {
        _logger = logger;

        // Qdrant cloud (sa-east-1, gRPC over TLS port 6334)
        var host    = config["Qdrant:Endpoint"]!;
        var apiKey  = config["Qdrant:ApiKey"]!;
        _qdrant = new QdrantClient(host: host, https: true, apiKey: apiKey, port: 6334);

        // Azure OpenAI embeddings (text-embedding-ada-002)
        var endpoint   = config["AI:AzureEmbeddingEndpoint"]!;
        var key        = config["AI:AzureEmbeddingApiKey"]!;
        var deployment = config["AI:AzureEmbeddingDeploymentName"] ?? "text-embedding-ada-002";
        var aoai = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));
        _embedder = aoai.GetEmbeddingClient(deployment);
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
            var embResult = await _embedder.GenerateEmbeddingAsync(embeddingQuery, cancellationToken: ct);
            var vector = embResult.Value.ToFloats();

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
