// AI.Infrastructure/Services/AzureDocumentIntelligenceExtractor.cs

using AI.Application.Common.Options;
using AI.Application.DTOs;
using AI.Application.Interfaces.Common;
using Azure;
using Azure.AI.DocumentIntelligence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

namespace AI.Infrastructure.Services;

public sealed class AzureDocumentIntelligenceExtractor : IDocumentExtractor
{
    private readonly DocumentIntelligenceClient _client;
    private readonly ILogger<AzureDocumentIntelligenceExtractor> _logger;

    public AzureDocumentIntelligenceExtractor(
        IOptions<AzureDocumentIntelligenceOptions> options,
        ILogger<AzureDocumentIntelligenceExtractor> logger)
    {
        var opt = options.Value;
        _client = new DocumentIntelligenceClient(
            new Uri(opt.Endpoint),
            new AzureKeyCredential(opt.ApiKey));
        _logger = logger;
    }

    public async Task<DocumentExtractionResult> ExtractAsync(
        Stream pdfStream,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Sending PDF to Azure Document Intelligence (prebuilt-layout)");

        // ── 1. Read stream into bytes ────────────────────────────────────────
        // ✅ Stable 1.0.0 — no AnalyzeDocumentContent, use BinaryData directly
        using var ms = new MemoryStream();
        await pdfStream.CopyToAsync(ms, cancellationToken);
        var pdfBytes = ms.ToArray();

        // ── 2. Send to Azure ─────────────────────────────────────────────────
        var operation = await _client.AnalyzeDocumentAsync(
            WaitUntil.Completed,
            "prebuilt-layout",
            BinaryData.FromBytes(pdfBytes),   // ✅ replaces AnalyzeDocumentContent
            cancellationToken: cancellationToken);

        var result = operation.Value;

        _logger.LogInformation(
            "Azure extraction complete: {PageCount} pages detected",
            result.Pages.Count);

        // ── 3. Build pages and tables ────────────────────────────────────────
        var pages = BuildPages(result);
        var tables = BuildTables(result);
        var mergedPages = MergeTablesIntoPages(pages, tables);

        return new DocumentExtractionResult(
            TotalPages: result.Pages.Count,
            Pages: mergedPages);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Page extraction
    // ────────────────────────────────────────────────────────────────────────

    private static List<ExtractedPage> BuildPages(AnalyzeResult result)
    {
        var pages = new List<ExtractedPage>();

        foreach (var page in result.Pages)
        {
            var sb = new StringBuilder();

            var paragraphsOnPage = result.Paragraphs?
                .Where(p => p.BoundingRegions != null &&
                            p.BoundingRegions.Any(r => r.PageNumber == page.PageNumber))
                .OrderBy(p => p.BoundingRegions!.First().Polygon[1])
                .ToList() ?? [];

            foreach (var paragraph in paragraphsOnPage)
            {
                // ✅ Stable 1.0.0 — DocumentParagraphRole is a struct, use ==
                if (paragraph.Role == "pageHeader" ||
     paragraph.Role == "pageFooter" ||
     paragraph.Role == "pageNumber")
                    continue;

                sb.AppendLine(paragraph.Content);
            }

            var text = sb.ToString().Trim();

            if (!string.IsNullOrWhiteSpace(text))
            {
                pages.Add(new ExtractedPage(
                    PageNumber: page.PageNumber,
                    Text: text,
                    Tables: new List<ExtractedTable>()));
            }
        }

        return pages;
    }

    // ────────────────────────────────────────────────────────────────────────
    // Table extraction
    // ────────────────────────────────────────────────────────────────────────

    private static List<ExtractedTable> BuildTables(AnalyzeResult result)
    {
        var tables = new List<ExtractedTable>();
        if (result.Tables is null) return tables;

        foreach (var table in result.Tables)
        {
            var pageNumber = table.BoundingRegions != null &&
                             table.BoundingRegions.Count > 0
                ? table.BoundingRegions[0].PageNumber
                : 0;

            tables.Add(new ExtractedTable(
                PageNumber: pageNumber,
                MarkdownContent: ConvertTableToMarkdown(table)));
        }

        return tables;
    }

    private static string ConvertTableToMarkdown(DocumentTable table)
    {
        var sb = new StringBuilder();
        var rowCount = table.RowCount;
        var colCount = table.ColumnCount;

        var grid = new string[rowCount, colCount];
        foreach (var cell in table.Cells)
            grid[cell.RowIndex, cell.ColumnIndex] = cell.Content.Replace("\n", " ");

        // Header
        sb.Append("| ");
        for (var c = 0; c < colCount; c++) sb.Append($"{grid[0, c]} | ");
        sb.AppendLine();

        // Separator
        sb.Append("| ");
        for (var c = 0; c < colCount; c++) sb.Append("--- | ");
        sb.AppendLine();

        // Data rows
        for (var r = 1; r < rowCount; r++)
        {
            sb.Append("| ");
            for (var c = 0; c < colCount; c++) sb.Append($"{grid[r, c]} | ");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    // ────────────────────────────────────────────────────────────────────────
    // Merge tables into pages
    // ────────────────────────────────────────────────────────────────────────

    private static IReadOnlyList<ExtractedPage> MergeTablesIntoPages(
        List<ExtractedPage> pages,
        List<ExtractedTable> tables)
    {
        return pages.Select(page =>
        {
            var pageTables = tables
                .Where(t => t.PageNumber == page.PageNumber)
                .ToList();

            if (pageTables.Count == 0) return page;

            var tableText = string.Join("\n\n",
                pageTables.Select(t => $"[TABLE]\n{t.MarkdownContent}[/TABLE]"));

            // ✅ with expression uses = not :
            return page with
            {
                Text = $"{page.Text}\n\n{tableText}",
                Tables = pageTables
            };
        }).ToList();
    }
}