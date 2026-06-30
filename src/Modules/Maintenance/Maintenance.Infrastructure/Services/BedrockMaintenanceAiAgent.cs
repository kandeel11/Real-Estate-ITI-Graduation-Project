using System.Text;
using System.Text.Json;
using Maintenance.Application.DTOs.Responses;
using Maintenance.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Maintenance.Infrastructure.Services;

/// <summary>
/// Maintenance AI agent — calls the ITI Student API (DeepSeek v3.2).
/// Searches Qdrant for contract clauses, then asks the model to decide responsibility.
/// </summary>
public sealed class StudentApiMaintenanceAgent : IAiEvaluationService
{
    private const string ModelId = "deepseek.v3.2";
    private const string ChatPath = "student/chat";

    private const string SystemPrompt = """
        You are a Property Maintenance Ticket Qualifier.

        TASK
        Determine responsibility for a maintenance issue using the contract clauses provided in the user message.

        MANDATORY PROCESS
        1. Base the decision on the retrieved contract clauses and the fallback rules below.
        2. NEVER return "Unknown" when clauses are returned.

        DECISION LOGIC

        A. CONTRACT EXISTS (one or more clauses provided in the user message)

        1. Explicit Assignment
           If a clause explicitly assigns responsibility for the issue → use that assignment.
           Confidence: 90-100.

        2. Contract Silent (Fallback Rules)
           If the contract exists but does not explicitly address the exact issue:
           • Structural defects, building infrastructure, major plumbing faults, major electrical faults,
             burst pipes, water supply lines, drainage failures → Landlord.
           • Tenant misuse, negligence, accidental damage, or minor wear-and-tear → Tenant.
           Confidence: 60-89.
           Reasoning must include: "العقد لم ينص صراحة على هذا العطل، وبناءً على القواعد العامة..."

        B. CONTRACT NOT FOUND
        Only when the message explicitly says no contract clauses were found.
        Return: responsibleParty = "Unknown", confidenceScore = 0.
        Reasoning must explain in Arabic that no contract data was found.

        SPECIAL RULES
        • Do NOT treat monthly maintenance fees as assigning responsibility for major repairs.
        • A burst pipe is ALWAYS a major plumbing fault → Landlord.
        • Plumbing may only be Tenant responsibility only if a clause explicitly says so.

        OUTPUT
        Return ONLY a valid raw JSON object — no markdown, no code fences, no extra text.

        {
          "responsibleParty": "Landlord | Tenant | Unknown",
          "category": "<MUST be exactly one name from the Available categories list>",
          "confidenceScore": <0-100>,
          "reasoning": "<explanation in the contract language — Arabic if Arabic contract>"
        }

        CATEGORY RULES
        • The "category" field MUST be copied verbatim from the "Available categories" list in the user message.
        • Do NOT invent, translate, or modify the category name.
        • If unsure which category fits, pick the closest one from the list.

        LANGUAGE RULES
        • Detect the primary language of the retrieved contract clauses.
        • Generate reasoning in the same language as the contract (Arabic → Arabic reasoning).
        • responsibleParty must be exactly: "Landlord", "Tenant", or "Unknown".
        • When no clauses are found, write reasoning in the ticket's language.
        """;

    private readonly IContractClauseSearchService _search;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<StudentApiMaintenanceAgent> _logger;

    public StudentApiMaintenanceAgent(
        IContractClauseSearchService search,
        IHttpClientFactory httpClientFactory,
        ILogger<StudentApiMaintenanceAgent> logger)
    {
        _search = search;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<AiEvaluationResultDto> EvaluateAsync(
        string ticketNumber, string leaseId,
        string description, string imageUrl,
        IReadOnlyList<string> categoryNames,
        CancellationToken ct = default)
    {
        // 1. Retrieve relevant contract clauses from Qdrant
        var clauses = await _search.SearchAsync(leaseId, description, topK: 5, ct);

        // 2. Build the user message (image URL included as text — DeepSeek v3.2 is text-only)
        var clauseSection = clauses.Count > 0
            ? "Retrieved Contract Clauses:\n" + string.Join("\n---\n", clauses)
            : "No contract clauses found in the database for this lease.";

        var imageNote = string.IsNullOrWhiteSpace(imageUrl)
            ? string.Empty
            : $"\nAttached image URL (for context): {imageUrl}\n";

        // 3. Include exact category names so the AI returns a precise match
        var categoryList = categoryNames.Count > 0
            ? "Available categories (use EXACTLY one of these as the 'category' field):\n" +
              string.Join(", ", categoryNames)
            : string.Empty;

        var userText = $"""
            Ticket #{ticketNumber}

            Maintenance Issue Description:
            {description}
            {imageNote}
            {categoryList}

            {clauseSection}

            Please analyze this issue and determine the responsible party.
            """;

        // 3. Build the API request body
        var payload = JsonSerializer.Serialize(new
        {
            model_id = ModelId,
            messages = new[] { new { role = "user", content = userText } },
            system_prompt = SystemPrompt,
            max_tokens = 1024
        });

        _logger.LogInformation(
            "Calling ITI Student API ({Model}) for ticket {TicketNumber} — clauses: {Count}",
            ModelId, ticketNumber, clauses.Count);

        // 4. POST using the named HttpClient (Authorization header is pre-configured)
        using var client = _httpClientFactory.CreateClient("ITIStudentApi");
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(ChatPath, content, ct);
        response.EnsureSuccessStatusCode();

        var raw = await response.Content.ReadAsStringAsync(ct);
        return ParseResponse(raw, ticketNumber);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private AiEvaluationResultDto ParseResponse(string raw, string ticketNumber)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            // Try OpenAI-compatible format: choices[0].message.content
            string? text = null;
            if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                text = choices[0].GetProperty("message").GetProperty("content").GetString();

            // Fallbacks for custom response shapes
            if (text is null && root.TryGetProperty("content", out var contentEl))
                text = contentEl.ValueKind == JsonValueKind.String
                    ? contentEl.GetString()
                    : contentEl.GetRawText();

            if (text is null && root.TryGetProperty("output", out var outputEl))
                text = outputEl.GetString();

            if (text is null && root.TryGetProperty("text", out var textEl))
                text = textEl.GetString();

            if (text is null && root.TryGetProperty("output_text", out var outputTextEl))
                text = outputTextEl.GetString();

            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidOperationException("No text content found in API response.");

            // Strip accidental markdown fences
            var json = text.Trim();
            if (json.StartsWith("```"))
            {
                json = json[(json.IndexOf('\n') + 1)..];
                if (json.EndsWith("```")) json = json[..json.LastIndexOf("```")];
                json = json.Trim();
            }

            using var inner = JsonDocument.Parse(json);
            var r = inner.RootElement;
            return new AiEvaluationResultDto(
                ResponsibleParty: r.GetProperty("responsibleParty").GetString() ?? "Unknown",
                Category: r.GetProperty("category").GetString() ?? string.Empty,
                ConfidenceScore: r.GetProperty("confidenceScore").GetInt32(),
                Reasoning: r.GetProperty("reasoning").GetString() ?? string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to parse Student API response for ticket {TicketNumber}. Raw: {Raw}",
                ticketNumber, raw);
            return new AiEvaluationResultDto(
                "Unknown", "Parse Error", 0, "فشل في تحليل رد الذكاء الاصطناعي.");
        }
    }
}
