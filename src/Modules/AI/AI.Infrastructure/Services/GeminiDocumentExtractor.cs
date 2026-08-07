// AI.Infrastructure/Services/GeminiDocumentExtractor.cs

using System.Text.Json;
using System.Text.Json.Serialization;
using AI.Application.Common.Options;
using AI.Application.DTOs;
using AI.Application.Interfaces.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AI.Infrastructure.Services;

/// <summary>
/// Lease-document OCR/layout extraction via Gemini's multimodal vision model,
/// replacing Azure Document Intelligence's "prebuilt-layout" model after the
/// Azure subscription was disabled. Gemini reads the PDF directly (no
/// specialized layout model), so quality depends on prompt phrasing rather
/// than a purpose-built OCR pipeline — verify results on real lease PDFs.
/// </summary>
public sealed class GeminiDocumentExtractor : IDocumentExtractor
{
    private const string Prompt = """
        Extract the full text content of this PDF, page by page. If a page
        contains a table, render that table as GitHub-flavored markdown and
        include it inline within the page's text (do not drop tables). Return
        ONLY a JSON array with no surrounding prose, in this exact shape:
        [{"pageNumber": 1, "text": "...", "tables": [{"markdownContent": "..."}]}]
        Omit the "tables" array (or leave it empty) for pages with no tables.
        """;

    private readonly GeminiVisionClient _client;
    private readonly ILogger<GeminiDocumentExtractor> _logger;

    public GeminiDocumentExtractor(
        IHttpClientFactory httpClientFactory,
        IOptions<AiOptions> options,
        ILogger<GeminiDocumentExtractor> logger)
    {
        var o = options.Value;
        _client = new GeminiVisionClient(
            httpClientFactory.CreateClient(nameof(GeminiDocumentExtractor)),
            o.GeminiApiKey,
            o.GeminiVisionModel);
        _logger = logger;
    }

    public async Task<DocumentExtractionResult> ExtractAsync(
        Stream pdfStream,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending PDF to Gemini ({Model}) for layout extraction", nameof(GeminiDocumentExtractor));

        using var ms = new MemoryStream();
        await pdfStream.CopyToAsync(ms, cancellationToken);
        var pdfBytes = ms.ToArray();

        var json = await _client.GenerateJsonAsync(Prompt, pdfBytes, "application/pdf", cancellationToken);
        var geminiPages = JsonSerializer.Deserialize<List<GeminiPageResult>>(json)
            ?? throw new InvalidOperationException("Gemini returned an unparsable extraction result.");

        _logger.LogInformation("Gemini extraction complete: {PageCount} pages detected", geminiPages.Count);

        var pages = geminiPages.Select(p => new ExtractedPage(
            PageNumber: p.PageNumber,
            Text: p.Text,
            Tables: (p.Tables ?? [])
                .Select(t => new ExtractedTable(p.PageNumber, t.MarkdownContent))
                .ToList()))
            .ToList();

        return new DocumentExtractionResult(TotalPages: pages.Count, Pages: pages);
    }

    private sealed class GeminiPageResult
    {
        [JsonPropertyName("pageNumber")]
        public int PageNumber { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("tables")]
        public List<GeminiTableResult>? Tables { get; set; }
    }

    private sealed class GeminiTableResult
    {
        [JsonPropertyName("markdownContent")]
        public string MarkdownContent { get; set; } = string.Empty;
    }
}
