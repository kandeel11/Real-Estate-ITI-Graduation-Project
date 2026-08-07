// AI.Infrastructure/EventHandlers/LeaseActivatedEventHandler.cs

using AI.Application.Interfaces.Common;
using Document.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Embeddings;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace AI.Infrastructure.EventHandlers;

/// <summary>
/// Mohamed subscribes to Yasser's LeaseActivatedEvent.
/// Embeds the lease PDF text into Qdrant for RAG chatbot.
/// </summary>
public sealed class LeaseActivatedEventHandler
    : INotificationHandler<LeaseActivatedEvent>
{
    private readonly IDocumentExtractor _extractor;
    private readonly IDocumentStorageService _storage;
    private readonly ITextEmbeddingGenerationService _embeddings;
    private readonly QdrantClient _qdrant;
    private readonly ILogger<LeaseActivatedEventHandler> _logger;

    private const string CollectionName = "contracts";
    private const int EmbeddingDimension = 768; // Gemini text-embedding-004 (was 1536 for Azure ada-002)
    private const int ChunkSize = 150;
    private const int ChunkOverlap = 20;

    public LeaseActivatedEventHandler(
        IDocumentExtractor extractor,
        IDocumentStorageService storage,
        ITextEmbeddingGenerationService embeddings,
        QdrantClient qdrant,
        ILogger<LeaseActivatedEventHandler> logger)
    {
        _extractor = extractor;
        _storage = storage;
        _embeddings = embeddings;
        _qdrant = qdrant;
        _logger = logger;
    }

    public async Task Handle(
        LeaseActivatedEvent notification,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "AI received LeaseActivatedEvent | LeaseId={LeaseId} — embedding into Qdrant",
            notification.LeaseId);

        try
        {
            // ── 1. Download activated lease PDF ──────────────────────────────
            var pdfBytes = await _storage.DownloadAsync(
                notification.BlobPath, cancellationToken);

            // ── 2. OCR via Azure DI ───────────────────────────────────────────
            using var ms = new MemoryStream(pdfBytes);
            var extraction = await _extractor.ExtractAsync(ms, cancellationToken);

            // ── 3. Chunk + embed + upsert into Qdrant ─────────────────────────
            await EnsureCollectionAsync(cancellationToken);

            var chunks = ChunkPages(extraction.Pages, notification.LeaseId);
            await EmbedAndUpsertAsync(chunks, notification.LeaseId, cancellationToken);

            _logger.LogInformation(
                "Lease {LeaseId} embedded into Qdrant | {Count} chunks",
                notification.LeaseId, chunks.Count);
        }
        catch (Exception ex)
        {
            // Non-critical — RAG embedding failure should not break the flow
            _logger.LogError(ex,
                "Failed to embed lease {LeaseId} into Qdrant", notification.LeaseId);
        }
    }

    // ── Chunking ──────────────────────────────────────────────────────────────

    private static List<(Guid ChunkId, string Text, int Page, int Index)> ChunkPages(
        IReadOnlyList<AI.Application.DTOs.ExtractedPage> pages,
        Guid leaseId)
    {
        var chunks = new List<(Guid, string, int, int)>();
        var chunkIndex = 0;

        foreach (var page in pages)
        {
            var words = page.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            for (var start = 0; start < words.Length; start += ChunkSize - ChunkOverlap)
            {
                var slice = words.Skip(start).Take(ChunkSize).ToArray();
                if (slice.Length < 5) break;

                chunks.Add((Guid.NewGuid(), string.Join(' ', slice), page.PageNumber, chunkIndex++));

                if (start + ChunkSize >= words.Length) break;
            }
        }

        return chunks;
    }

    // ── Embed + Upsert ────────────────────────────────────────────────────────

    private async Task EmbedAndUpsertAsync(
        List<(Guid ChunkId, string Text, int Page, int Index)> chunks,
        Guid leaseId,
        CancellationToken cancellationToken)
    {
        foreach (var batch in chunks.Chunk(100))
        {
            var texts = batch.Select(c => c.Text).ToList();
            var embeddings = await _embeddings.GenerateEmbeddingsAsync(
                texts, cancellationToken: cancellationToken);

            var points = batch.Zip(embeddings, (chunk, vector) =>
                new PointStruct
                {
                    Id = new PointId { Uuid = chunk.ChunkId.ToString() },
                    Vectors = new Vectors { Vector = new Vector { Data = { vector.ToArray() } } },
                    Payload =
                    {
                        ["contractId"] = leaseId.ToString(),
                        ["pageNumber"] = chunk.Page,
                        ["chunkIndex"] = chunk.Index,
                        ["text"]       = chunk.Text
                    }
                }).ToList();

            await _qdrant.UpsertAsync(CollectionName, points,
                cancellationToken: cancellationToken);
        }
    }

    private async Task EnsureCollectionAsync(CancellationToken ct)
    {
        if (await _qdrant.CollectionExistsAsync(CollectionName, ct)) return;

        await _qdrant.CreateCollectionAsync(CollectionName,
            new VectorParams { Size = EmbeddingDimension, Distance = Distance.Cosine }, cancellationToken: ct);
    }
}