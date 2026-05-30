// AI.Infrastructure/Services/ContractDocumentService.cs

using AI.Application.DTOs;
using AI.Application.Interfaces;
using AI.Application.Interfaces.Common;
using AI.Application.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Microsoft.SemanticKernel.Embeddings;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using System.Diagnostics;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace AI.Infrastructure.Services;

/// <summary>
/// Orchestrates the full RAG ingestion pipeline for contract PDFs:
///   1. Extract text page-by-page  (PdfPig)
///   2. Chunk with sliding window   (token-aware)
///   3. Embed each chunk            (OpenAI text-embedding-3-small via SK)
///   4. Upsert vectors into Qdrant
/// </summary>
public sealed class ContractDocumentService : IContractDocumentService
{
    // ── Qdrant vector dimension for text-embedding-3-small ──────────────────
    private const int EmbeddingDimension = 1536;

    private readonly QdrantClient _qdrant;
    private readonly ITextEmbeddingGenerationService _embeddings;
    private readonly ILogger<ContractDocumentService> _logger;

    public ContractDocumentService(
        QdrantClient qdrant,
        ITextEmbeddingGenerationService embeddings,
        ILogger<ContractDocumentService> logger)
    {
        _qdrant = qdrant;
        _embeddings = embeddings;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────────────────────
    // Public API
    // ────────────────────────────────────────────────────────────────────────

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

        // ── Step 1: Ensure the Qdrant collection exists ──────────────────────
        await EnsureCollectionAsync(request.CollectionName, cancellationToken);

        // ── Step 2: Extract text from PDF ────────────────────────────────────
        var (pages, totalPages) = ExtractPages(pdfStream, request.ContractId);

        // ── Step 3: Chunk text with sliding window ───────────────────────────
        var chunks = ChunkPages(pages, request);

        _logger.LogInformation(
            "Contract {ContractId}: {Pages} pages → {Chunks} chunks",
            request.ContractId, totalPages, chunks.Count);

        // ── Step 4: Embed + upsert in batches ────────────────────────────────
        var upserted = await EmbedAndUpsertAsync(
            chunks, request.CollectionName, cancellationToken);

        sw.Stop();

        _logger.LogInformation(
            "Ingestion complete for {ContractId}: {Upserted} vectors in {Elapsed}ms",
            request.ContractId, upserted, sw.ElapsedMilliseconds);

        return new ContractIngestionResult(
            ContractId: request.ContractId,
            TotalPages: totalPages,
            TotalChunks: chunks.Count,
            UpsertedVectors: upserted,
            Duration: sw.Elapsed);
    }

    public async Task DeleteAsync(
        Guid contractId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting vectors for contract {ContractId}", contractId);

        // Delete by payload filter so we don't need to track IDs externally
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

    // ────────────────────────────────────────────────────────────────────────
    // Step 1 — PDF text extraction (PdfPig, page-by-page)
    // ────────────────────────────────────────────────────────────────────────

    private static (List<(int PageNumber, string Text)> Pages, int TotalPages)
        ExtractPages(Stream pdfStream, Guid contractId)
    {
        using var document = PdfDocument.Open(pdfStream);

        var pages = document.GetPages()
            .Select(p => (
                PageNumber: p.Number,
                Text: string.Concat(p.GetWords().Select(w => w.Text + " ")).Trim()))
            .Where(p => !string.IsNullOrWhiteSpace(p.Text))
            .ToList();

        return (pages, document.NumberOfPages);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Step 2 — Sliding-window chunking
    // ────────────────────────────────────────────────────────────────────────

    private static List<ContractChunk> ChunkPages(
        List<(int PageNumber, string Text)> pages,
        ContractIngestionRequest request)
    {
        var chunks = new List<ContractChunk>();
        var chunkIndex = 0;

        foreach (var (pageNumber, text) in pages)
        {
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            for (var start = 0; start < words.Length; start += request.ChunkSize - request.ChunkOverlap)
            {
                var slice = words
                    .Skip(start)
                    .Take(request.ChunkSize)
                    .ToArray();

                if (slice.Length == 0) break;

                chunks.Add(new ContractChunk(
                    ChunkId: Guid.NewGuid(),
                    ContractId: request.ContractId,
                    PageNumber: pageNumber,
                    ChunkIndex: chunkIndex++,
                    Text: string.Join(' ', slice)));

                // Reached the last slice for this page
                if (start + request.ChunkSize >= words.Length) break;
            }
        }

        return chunks;
    }

    // ────────────────────────────────────────────────────────────────────────
    // Step 3 — Embed + upsert in batches of 100
    // ────────────────────────────────────────────────────────────────────────

    private const int BatchSize = 100;

    private async Task<int> EmbedAndUpsertAsync(
        List<ContractChunk> chunks,
        string collectionName,
        CancellationToken cancellationToken)
    {
        var totalUpserted = 0;

        foreach (var batch in chunks.Chunk(BatchSize))
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Generate embeddings for the whole batch in one API call
            var texts = batch.Select(c => c.Text).ToList();
            var embeddings = await _embeddings.GenerateEmbeddingsAsync(
                texts, cancellationToken: cancellationToken);

            // Build Qdrant points
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

            await _qdrant.UpsertAsync(
                collectionName: collectionName,
                points: points,
                cancellationToken: cancellationToken);

            totalUpserted += points.Count;

            _logger.LogDebug(
                "Upserted batch of {Count} vectors (total so far: {Total})",
                points.Count, totalUpserted);
        }

        return totalUpserted;
    }

    // ────────────────────────────────────────────────────────────────────────
    // Helpers
    // ────────────────────────────────────────────────────────────────────────

    private async Task EnsureCollectionAsync(
        string collectionName,
        CancellationToken cancellationToken)
    {
        var exists = await _qdrant.CollectionExistsAsync(
            collectionName, cancellationToken);

        if (exists) return;

        _logger.LogInformation(
            "Creating Qdrant collection '{Collection}'", collectionName);

        await _qdrant.CreateCollectionAsync(
     collectionName: collectionName,
     vectorsConfig: new VectorParams
     {
         Size = EmbeddingDimension,
         Distance = Distance.Cosine
     },
     cancellationToken: cancellationToken);
    }
}