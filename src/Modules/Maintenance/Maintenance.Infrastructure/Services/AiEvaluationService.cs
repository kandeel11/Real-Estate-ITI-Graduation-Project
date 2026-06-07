using System.Text;
using System.Text.Json;
using Maintenance.Application.DTOs.Responses;
using Maintenance.Application.Interfaces;

namespace Maintenance.Infrastructure.Services;

public class AiEvaluationService(IHttpClientFactory httpClientFactory) : IAiEvaluationService
{
    private const string WebhookUrl =
        "https://mohamedkhaleed8080.app.n8n.cloud/webhook/ticket-evaluate";

    public async Task<AiEvaluationResultDto> EvaluateAsync(
        string ticketNumber, string leaseId,
        string description, string imageUrl,
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
        using var doc = JsonDocument.Parse(raw);
        var output = doc.RootElement.GetProperty("output");

        return new AiEvaluationResultDto(
            output.GetProperty("responsibleParty").GetString() ?? "Owner",
            output.GetProperty("category").GetString() ?? string.Empty,
            output.GetProperty("confidenceScore").GetInt32(),
            output.GetProperty("reasoning").GetString() ?? string.Empty);
    }
}