using System.Text;
using System.Text.Json;
using Maintenance.Application.DTOs.Responses;
using Maintenance.Application.Interfaces;

namespace Maintenance.Infrastructure.Services;

public class AiEvaluationService(IHttpClientFactory httpClientFactory) : IAiEvaluationService
{
    private const string WebhookUrl =
        "https://mkhaled2626.app.n8n.cloud/webhook/ticket-evaluate";

    public async Task<AiEvaluationResultDto> EvaluateAsync(
        string ticketNumber, string leaseId,
        string description, string imageUrl,
        IReadOnlyList<string> categoryNames,
        CancellationToken ct = default)
    {
        var client = httpClientFactory.CreateClient("AiEvaluation");

        var payload = new
        {
            ticketId = ticketNumber,
            leaseId = leaseId,
            userDescription = description,
            imageUrl = imageUrl
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(WebhookUrl, content, ct);
        response.EnsureSuccessStatusCode();

        var raw = await response.Content.ReadAsStringAsync(ct);

        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException(
                "n8n webhook returned an empty body. Ensure the workflow has a 'Respond to Webhook' node that returns the AI result as JSON.");

        using var doc = JsonDocument.Parse(raw);

        // n8n may wrap the result in an array (batch mode) — unwrap if needed
        var root = doc.RootElement.ValueKind == JsonValueKind.Array
            ? doc.RootElement[0]
            : doc.RootElement;

        var output = root.TryGetProperty("output", out var outEl) ? outEl : root;

        return new AiEvaluationResultDto(
            output.GetProperty("responsibleParty").GetString() ?? "Owner",
            output.GetProperty("category").GetString() ?? string.Empty,
            output.GetProperty("confidenceScore").GetInt32(),
            output.GetProperty("reasoning").GetString() ?? string.Empty);
    }
}