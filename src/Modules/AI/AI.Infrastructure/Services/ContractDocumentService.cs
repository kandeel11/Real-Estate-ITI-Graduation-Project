// AI.Infrastructure/Services/ContractDocumentService.cs

using AI.Application.DTOs;
using AI.Application.Interfaces.Common;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Embeddings;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using System.Diagnostics;

namespace AI.Infrastructure.Services;

public sealed class ContractDocumentService : IContractDocumentService
{
    private const int EmbeddingDimension = 1536;
    private const int BatchSize = 100;

    private readonly QdrantClient _qdrant;
    private readonly ITextEmbeddingGenerationService _embeddings;
    private readonly IDocumentExtractor _extractor;   // ← injected
    private readonly ILogger<ContractDocumentService> _logger;

    public ContractDocumentService(
        QdrantClient qdrant,
        ITextEmbeddingGenerationService embeddings,
        IDocumentExtractor extractor,                   // ← NEW
        ILogger<ContractDocumentService> logger)
    {
        _qdrant = qdrant;
        _embeddings = embeddings;
        _extractor = extractor;
        _logger = logger;
    }

    public async Task<ContractIngestionResult> IngestAsync(
        Stream pdfStream,
        ContractIngestionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pdfStream);
        ArgumentNullException.ThrowIfNull(request);

        var sw = Stopwatch.StartNew();

        _logger.LogInformation(
            "Starting ingestion for contract {ContractId} ({FileName})",
            request.ContractId, request.FileName);

        // ── Step 1: Ensure Qdrant collection ─────────────────────────────────
        await EnsureCollectionAsync(request.CollectionName, cancellationToken);

        // ── Step 2: Extract text via Azure Document Intelligence ─────────────
        var extraction = await _extractor.ExtractAsync(pdfStream, cancellationToken);

        _logger.LogInformation(
            "Extracted {Pages} pages from contract {ContractId}",
            extraction.TotalPages, request.ContractId);

        // ── Step 3: Chunk with sliding window ────────────────────────────────
        var chunks = ChunkPages(extraction.Pages, request);

        _logger.LogInformation(
            "Contract {ContractId}: {Pages} pages → {Chunks} chunks",
            request.ContractId, extraction.TotalPages, chunks.Count);

        // ── Step 4: Embed + upsert ────────────────────────────────────────────
        var upserted = await EmbedAndUpsertAsync(
            chunks, request.CollectionName, cancellationToken);

        sw.Stop();

        return new ContractIngestionResult(
            ContractId: request.ContractId,
            TotalPages: extraction.TotalPages,
            TotalChunks: chunks.Count,
            UpsertedVectors: upserted,
            Duration: sw.Elapsed);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Chunking — now works on ExtractedPage instead of raw string
    // ────────────────────────────────────────────────────────────────────────

    private static List<ContractChunk> ChunkPages(
        IReadOnlyList<ExtractedPage> pages,
        ContractIngestionRequest request)
    {
        var chunks = new List<ContractChunk>();
        var chunkIndex = 0;

        foreach (var page in pages)
        {
            var words = page.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            for (var start = 0; start < words.Length; start += request.ChunkSize - request.ChunkOverlap)
            {
                var slice = words.Skip(start).Take(request.ChunkSize).ToArray();
                if (slice.Length == 0) break;

                chunks.Add(new ContractChunk(
                    ChunkId: Guid.NewGuid(),
                    ContractId: request.ContractId,
                    PageNumber: page.PageNumber,
                    ChunkIndex: chunkIndex++,
                    Text: string.Join(' ', slice)));

                if (start + request.ChunkSize >= words.Length) break;
            }
        }

        return chunks;
    }

    private async Task<int> EmbedAndUpsertAsync(
        List<ContractChunk> chunks,
        string collectionName,
        CancellationToken cancellationToken)
    {
        var totalUpserted = 0;

        foreach (var batch in chunks.Chunk(BatchSize))
        {
            cancellationToken.ThrowIfCancellationRequested();

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
                        ["contractId"] = chunk.ContractId.ToString(),
                        ["pageNumber"] = chunk.PageNumber,
                        ["chunkIndex"] = chunk.ChunkIndex,
                        ["text"]       = chunk.Text
                    }
                }).ToList();

            await _qdrant.UpsertAsync(collectionName, points, cancellationToken: cancellationToken);
            totalUpserted += points.Count;
        }

        return totalUpserted;
    }

    private async Task EnsureCollectionAsync(
        string collectionName,
        CancellationToken cancellationToken)
    {
        if (await _qdrant.CollectionExistsAsync(collectionName, cancellationToken)) return;

        await _qdrant.CreateCollectionAsync(
            collectionName: collectionName,
            vectorsConfig: new VectorParams
            {
                Size = EmbeddingDimension,
                Distance = Distance.Cosine
            },
            cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(Guid contractId, CancellationToken cancellationToken = default)
    {
        await _qdrant.DeleteAsync(
            collectionName: "contracts",
            filter: new Filter
            {
                Must =
                {
                    new Condition
                    {
                        Field = new FieldCondition
                        {
                            Key   = "contractId",
                            Match = new Match { Text = contractId.ToString() }
                        }
                    }
                }
            },
            cancellationToken: cancellationToken);
    }
}