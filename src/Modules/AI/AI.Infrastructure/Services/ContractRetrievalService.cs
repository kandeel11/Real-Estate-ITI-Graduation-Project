// AI.Infrastructure/Services/ContractRetrievalService.cs

using AI.Application.DTOs;
using AI.Application.Interfaces.Common;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Embeddings;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace AI.Infrastructure.Services;

/// <summary>
/// Converts a natural-language query into an embedding, then
/// queries Qdrant for the nearest contract chunks.
/// </summary>
public sealed class ContractRetrievalService : IContractRetrievalService
{
    private readonly QdrantClient _qdrant;
    private readonly ITextEmbeddingGenerationService _embeddings;
    private readonly ILogger<ContractRetrievalService> _logger;

    public ContractRetrievalService(
        QdrantClient qdrant,
        ITextEmbeddingGenerationService embeddings,
        ILogger<ContractRetrievalService> logger)
    {
        _qdrant = qdrant;
        _embeddings = embeddings;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ContractSearchResult>> SearchAsync(
     string query,
     string collectionName,
     int topK = 5,
     double minScore = 0.70,
     Guid? contractId = null,
     CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        _logger.LogInformation(
            "Semantic search in '{Collection}' | topK={TopK} | contractId={ContractId} | query={Query}",
            collectionName, topK, contractId, query);

        // ── 1. Embed the query ───────────────────────────────────────────────────
        var embeddings = await _embeddings.GenerateEmbeddingsAsync(
            [query], cancellationToken: cancellationToken);

        var queryVector = embeddings[0].ToArray();

        // ── 2. Build filter — only when contractId is provided ───────────────────
        // ✅ Use Match.Keyword (not Match.Text) for Keyword-indexed fields
        Filter? filter = contractId.HasValue
            ? new Filter
            {
                Must =
                {
                new Condition
                {
                    Field = new FieldCondition
                    {
                        Key   = "contractId",
                        Match = new Match
                        {
                            Keyword = contractId.Value.ToString()  // ✅ Keyword not Text
                        }
                    }
                }
                }
            }
            : null;

        // ── 3. Search Qdrant ─────────────────────────────────────────────────────
        var hits = await _qdrant.SearchAsync(
            collectionName: collectionName,
            vector: queryVector,
            filter: filter,
            limit: (ulong)topK,
            scoreThreshold: (float)minScore,
            cancellationToken: cancellationToken);

        // ── 4. Map results ───────────────────────────────────────────────────────
        var results = hits
            .OrderByDescending(h => h.Score)
            .Select(h => new ContractSearchResult(
                ContractId: Guid.Parse(h.Payload["contractId"].StringValue),
                PageNumber: (int)h.Payload["pageNumber"].IntegerValue,
                ChunkIndex: (int)h.Payload["chunkIndex"].IntegerValue,
                Text: h.Payload["text"].StringValue,
                Score: h.Score))
            .ToList();

        _logger.LogInformation(
            "Search returned {Count} results (minScore={MinScore})",
            results.Count, minScore);

        return results;
    }
}