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
    private const int EmbeddingDimension = 768; // Gemini text-embedding-004 (was 1536 for Azure ada-002)
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

    // AI.Infrastructure/Services/ContractDocumentService.cs

    // ────────────────────────────────────────────────────────────────────────────
    // Chunking — paragraph-aware, page-bound, token-efficient
    // ────────────────────────────────────────────────────────────────────────────

    private static List<ContractChunk> ChunkPages(
        IReadOnlyList<ExtractedPage> pages,
        ContractIngestionRequest request)
    {
        var chunks = new List<ContractChunk>();
        var chunkIndex = 0;

        foreach (var page in pages)
        {
            // ── Split page into paragraphs first ─────────────────────────────────
            // Respects natural paragraph breaks before word-level chunking
            var paragraphs = SplitIntoParagraphs(page.Text);

            foreach (var paragraph in paragraphs)
            {
                var words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                // Paragraph fits in one chunk — no splitting needed
                if (words.Length <= request.ChunkSize)
                {
                    if (words.Length == 0) continue;

                    chunks.Add(new ContractChunk(
                        ChunkId: Guid.NewGuid(),
                        ContractId: request.ContractId,
                        PageNumber: page.PageNumber,
                        ChunkIndex: chunkIndex++,
                        Text: string.Join(' ', words)));

                    continue;
                }

                // Paragraph too large — sliding window within this paragraph only
                // ✅ Never crosses page boundary
                for (var start = 0; start < words.Length; start += request.ChunkSize - request.ChunkOverlap)
                {
                    var slice = words
                        .Skip(start)
                        .Take(request.ChunkSize)
                        .ToArray();

                    if (slice.Length < 10) break;   // skip tiny trailing chunks

                    chunks.Add(new ContractChunk(
                        ChunkId: Guid.NewGuid(),
                        ContractId: request.ContractId,
                        PageNumber: page.PageNumber,
                        ChunkIndex: chunkIndex++,
                        Text: string.Join(' ', slice)));

                    if (start + request.ChunkSize >= words.Length) break;
                }
            }
        }

        return chunks;
    }

    /// <summary>
    /// Splits page text into meaningful paragraphs using blank lines,
    /// numbered clauses, and Arabic paragraph markers as boundaries.
    /// </summary>
    private static List<string> SplitIntoParagraphs(string text)
    {
        // Split on:
        // 1. Double newlines      → blank line between paragraphs
        // 2. Numbered clauses     → "1." "2." "أولاً" "ثانياً"
        // 3. [TABLE] markers      → table content stays separate
        var rawParagraphs = text
            .Split(
                new[] { "\n\n", "\r\n\r\n" },
                StringSplitOptions.RemoveEmptyEntries);

        var result = new List<string>();

        foreach (var para in rawParagraphs)
        {
            var trimmed = para.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            // Further split on Arabic clause markers
            var subParagraphs = SplitOnArabicClauses(trimmed);
            result.AddRange(subParagraphs.Where(p => !string.IsNullOrWhiteSpace(p)));
        }

        // Fallback: if no paragraph breaks found, treat whole page as one paragraph
        return result.Count > 0 ? result : [text.Trim()];
    }

    /// <summary>
    /// Splits on Arabic ordinal markers commonly found in contracts.
    /// e.g. "أولاً", "ثانياً", "البند الأول", "المادة الأولى"
    /// </summary>
    private static List<string> SplitOnArabicClauses(string text)
    {
        // Arabic contract clause markers
        var markers = new[]
        {
        "أولاً",  "ثانياً",  "ثالثاً",  "رابعاً",
        "خامساً", "سادساً",  "سابعاً",  "ثامناً",
        "البند",  "المادة",  "الفصل",   "الباب",
        "[TABLE]"
    };

        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var result = new List<string>();
        var buffer = new System.Text.StringBuilder();

        foreach (var line in lines)
        {
            var isMarker = markers.Any(m => line.TrimStart().StartsWith(m));

            if (isMarker && buffer.Length > 0)
            {
                result.Add(buffer.ToString().Trim());
                buffer.Clear();
            }

            buffer.AppendLine(line);
        }

        if (buffer.Length > 0)
            result.Add(buffer.ToString().Trim());

        return result.Count > 1 ? result : [text];
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
        var exists = await _qdrant.CollectionExistsAsync(collectionName, cancellationToken);

        if (!exists)
        {
            _logger.LogInformation("Creating Qdrant collection '{Collection}'", collectionName);

            await _qdrant.CreateCollectionAsync(
                collectionName: collectionName,
                vectorsConfig: new VectorParams
                {
                    Size = EmbeddingDimension,
                    Distance = Distance.Cosine
                },
                cancellationToken: cancellationToken);
        }

        // ✅ Always ensure indexes exist — runs even if collection already existed
        await EnsurePayloadIndexesAsync(collectionName, cancellationToken);
    }

    private async Task EnsurePayloadIndexesAsync(
        string collectionName,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Ensuring payload indexes on '{Collection}'", collectionName);

        // ✅ contractId — Keyword index (required for filter by contractId)
        await _qdrant.CreatePayloadIndexAsync(
            collectionName: collectionName,
            fieldName: "contractId",
            schemaType: PayloadSchemaType.Keyword,
            cancellationToken: cancellationToken);

        // ✅ pageNumber — Integer index
        await _qdrant.CreatePayloadIndexAsync(
            collectionName: collectionName,
            fieldName: "pageNumber",
            schemaType: PayloadSchemaType.Integer,
            cancellationToken: cancellationToken);

        // ✅ chunkIndex — Integer index
        await _qdrant.CreatePayloadIndexAsync(
            collectionName: collectionName,
            fieldName: "chunkIndex",
            schemaType: PayloadSchemaType.Integer,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Payload indexes ready on '{Collection}'", collectionName);
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