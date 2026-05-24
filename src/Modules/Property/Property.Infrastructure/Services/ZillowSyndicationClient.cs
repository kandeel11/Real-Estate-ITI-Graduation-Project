using Property.Application.Interfaces;

namespace Property.Infrastructure.Services;

public class ZillowSyndicationClient : IPropertySyndicationService
{
    private readonly HttpClient _httpClient;

    public ZillowSyndicationClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task SyndicateAsync(Guid propertyId, CancellationToken ct = default)
    {
        // Zillow API integration — sends property data to external portal
        var payload = new { propertyId };
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/listings", content, ct);
        response.EnsureSuccessStatusCode();
    }
}
