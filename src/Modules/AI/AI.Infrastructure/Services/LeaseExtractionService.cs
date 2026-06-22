// AI.Infrastructure/Services/LeaseExtractionService.cs

using AI.Application.DTOs;
using AI.Application.Interfaces;
using AI.Application.Interfaces.Common;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;

namespace AI.Infrastructure.Services;

/// <summary>
/// Owned by AI module — Leases module never touches Azure DI or Claude directly.
///
/// Step 1: IDocumentExtractor (Azure DI) OCRs the PDF
/// Step 2: Claude extracts structured fields with confidence scores
/// </summary>
public sealed class LeaseExtractionService : ILeaseExtractionService
{
    private readonly IDocumentExtractor _extractor;
    private readonly IChatCompletionService _chat;
    private readonly Kernel _kernel;
    private readonly ILogger<LeaseExtractionService> _logger;

    private const decimal ConfidenceThreshold = 80m;

    public LeaseExtractionService(
        IDocumentExtractor extractor,
        IChatCompletionService chat,
        Kernel kernel,
        ILogger<LeaseExtractionService> logger)
    {
        _extractor = extractor;
        _chat = chat;
        _kernel = kernel;
        _logger = logger;
    }

    public async Task<ExtractedLeaseData> ExtractAsync(
        byte[] pdfBytes,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting lease PDF extraction pipeline");

        // ── Step 1: OCR via Azure Document Intelligence ───────────────────────
        using var ms = new MemoryStream(pdfBytes);
        var extraction = await _extractor.ExtractAsync(ms, cancellationToken);

        // Combine all page texts into one document
        var fullText = string.Join("\n\n",
            extraction.Pages.Select(p => p.Text));

        _logger.LogInformation(
            "Azure DI OCR complete — {Pages} pages, {Length} characters",
            extraction.TotalPages, fullText.Length);

        // ── Step 2: Claude extracts structured fields with confidence scores ───
        var result = await ExtractFieldsWithClaudeAsync(fullText, cancellationToken);

        _logger.LogInformation(
            "Lease extraction complete — Overall confidence: {Score}%",
            result.OverallConfidence);

        return result;
    }

    // ─────────────────────────────────────────────────────────────────────────

    private async Task<ExtractedLeaseData> ExtractFieldsWithClaudeAsync(
        string contractText,
        CancellationToken cancellationToken)
    {
        var snippet = contractText.Length > 4000
            ? contractText[..4000]
            : contractText;

        var prompt = $$"""
    You are a lease contract data extraction specialist.
    Extract the following fields from the lease contract text.
    For each field provide the extracted value AND a confidence score (0–100).

    Contract Text:
    ===
    {{snippet}}
    ===

    Fields to extract:
    - installment_amount    : The value of ONE payment installment (number only, no currency symbol).
                              This could be a monthly, quarterly, semi-annual, or annual figure —
                              extract exactly what the contract states as a single payment.
    - payment_frequency     : How often payments are made. Must be exactly one of:
                              Monthly, Quarterly, SemiAnnually, Annually.
                              If ambiguous, infer from context (e.g. "monthly rent" → Monthly).
    - total_rent_amount     : Total contract value if EXPLICITLY stated in the text (number only).
                              If not explicitly written, return null.
    - duration_in_months    : Lease duration in months if EXPLICITLY stated (e.g. "12-month lease" → 12).
                              If not explicitly stated, return null (backend will calculate from dates).
    - security_deposit      : Security deposit amount (number only).
    - start_date            : Lease start date (YYYY-MM-DD).
    - end_date              : Lease end date (YYYY-MM-DD).
    - payment_day_of_month  : Day of month when each installment is due (1–31).
    - tenant_name           : Full name of tenant.
    - owner_name            : Full name of owner/landlord.

    Confidence scoring:
    - 95–100 : Explicitly stated, no ambiguity
    - 80–94  : Clearly implied
    - 60–79  : Some ambiguity — needs review
    -  0–59  : Not found or very uncertain

    For fields that are null (not found), use: { "value": null, "confidence": 0 }

    Return ONLY valid JSON — no markdown, no explanation:
    {
      "installment_amount"   : { "value": "60000",        "confidence": 97 },
      "payment_frequency"    : { "value": "SemiAnnually", "confidence": 95 },
      "total_rent_amount"    : { "value": "120000",       "confidence": 98 },
      "duration_in_months"   : { "value": "12",           "confidence": 90 },
      "security_deposit"     : { "value": "10000",        "confidence": 95 },
      "start_date"           : { "value": "2026-01-01",   "confidence": 97 },
      "end_date"             : { "value": "2026-12-31",   "confidence": 97 },
      "payment_day_of_month" : { "value": "1",            "confidence": 92 },
      "tenant_name"          : { "value": "John Smith",   "confidence": 99 },
      "owner_name"           : { "value": "Sara Ahmed",   "confidence": 99 },
      "overall_confidence"   : 96
    }
    """;

        var history = new ChatHistory();
        history.AddUserMessage(prompt);

        var response = await _chat.GetChatMessageContentAsync(
            history,
            kernel: _kernel,
            cancellationToken: cancellationToken);

        var raw = response.Content ?? "{}";
        return ParseResponse(raw);
    }

    private ExtractedLeaseData ParseResponse(string raw)
    {
        try
        {
            var json = raw.Replace("```json", "").Replace("```", "").Trim();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            ExtractedLeaseField ParseField(string key)
            {
                if (!root.TryGetProperty(key, out var el))
                    return new ExtractedLeaseField(null, 0, true);

                var value = el.TryGetProperty("value", out var v)
                    ? (v.ValueKind == JsonValueKind.Null ? null : v.ToString())
                    : null;
                var confidence = el.TryGetProperty("confidence", out var c) ? c.GetDecimal() : 0m;

                return new ExtractedLeaseField(value, confidence, confidence < ConfidenceThreshold);
            }

            var overall = root.TryGetProperty("overall_confidence", out var oc)
                ? oc.GetDecimal() : 0m;

            return new ExtractedLeaseData(
                InstallmentAmount:  ParseField("installment_amount"),
                PaymentFrequency:   ParseField("payment_frequency"),
                TotalRentAmount:    ParseField("total_rent_amount"),
                DurationInMonths:   ParseField("duration_in_months"),
                SecurityDeposit:    ParseField("security_deposit"),
                StartDate:          ParseField("start_date"),
                EndDate:            ParseField("end_date"),
                PaymentDayOfMonth:  ParseField("payment_day_of_month"),
                TenantName:         ParseField("tenant_name"),
                OwnerName:          ParseField("owner_name"),
                OverallConfidence:  overall,
                RawAiResponse:      raw);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Claude response: {Raw}", raw);
            var failed = new ExtractedLeaseField(null, 0, true);
            return new ExtractedLeaseData(
                failed, failed, failed, failed, failed, failed, failed, failed, failed, failed, 0, raw);
        }
    }
}
