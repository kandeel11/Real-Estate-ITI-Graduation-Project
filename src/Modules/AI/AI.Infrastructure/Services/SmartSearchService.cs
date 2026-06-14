// AI.Infrastructure/Services/SmartSearchService.cs

using AI.Application.DTOs;
using AI.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;

namespace AI.Infrastructure.Services;

/// <summary>
/// Uses Claude to parse Arabic and English natural language queries
/// into structured property search filters.
///
/// Examples handled:
///   "شقة غرفتين في المعادي بحد أقصى 8000 جنيه"
///   "2 bed apartment in Maadi max 8000"
///   "villa with pool furnished in Riyadh"
///   "مكتب للإيجار في وسط البلد بالقرب من المترو"
/// </summary>
public sealed class SmartSearchService : ISmartSearchService
{
    private readonly IChatCompletionService _chat;
    private readonly Kernel _kernel;
    private readonly ILogger<SmartSearchService> _logger;

    public SmartSearchService(
        IChatCompletionService chat,
        Kernel kernel,
        ILogger<SmartSearchService> logger)
    {
        _chat = chat;
        _kernel = kernel;
        _logger = logger;
    }

    public async Task<ParsedSearchFilters> ParseQueryAsync(
        string rawQuery,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Parsing search query: {Query}", rawQuery);

        var prompt = $$"""
            You are a real estate search query parser for a platform operating in Egypt, Saudi Arabia, and UAE.
            Parse the user's natural language search query into structured filters.
            The query may be in Arabic, English, or mixed.

            User Query: "{{rawQuery}}"

            Extraction rules:
            - propertyType  : "Apartment" | "Villa" | "Office" | "Shop" | "Studio" | "Duplex" | null
            - listingType   : "For Rent" | "For Sale" | null
            - city          : detected city name in English | null
            - district      : detected neighborhood/area in English | null
            - nearbyLandmark: e.g. "near university", "near metro", "near airport" | null
            - bedrooms      : integer number of bedrooms | null
            - bathrooms     : integer number of bathrooms | null
            - minArea       : minimum area in sqm | null
            - maxArea       : maximum area in sqm | null
            - floor         : specific floor number | null
            - minPrice      : minimum price (number only) | null
            - maxPrice      : maximum price (number only) | null
            - amenities     : array from ["pool", "gym", "parking", "furnished",
                              "unfurnished", "pet_friendly", "garden", "balcony",
                              "elevator", "security", "central_ac", "near_school",
                              "near_university", "near_metro", "near_mall"] | []
            - confidence    : 0.0–1.0 how confident you are in the parsing
            - detectedLanguage: "ar" | "en" | "mixed"

            Arabic keywords reference:
            - شقة = Apartment, فيلا = Villa, مكتب = Office, محل = Shop
            - للإيجار = For Rent, للبيع = For Sale
            - غرف/غرفة = bedrooms, حمام = bathroom
            - مفروشة = furnished, بدون فرش = unfurnished
            - حمام سباحة = pool, موقف = parking, مصعد = elevator
            - بالقرب من = near, بجوار = next to

            Return ONLY valid JSON — no markdown, no explanation:
            {
              "propertyType"   : "Apartment",
              "listingType"    : "For Rent",
              "city"           : "Cairo",
              "district"       : "Maadi",
              "nearbyLandmark" : null,
              "bedrooms"       : 2,
              "bathrooms"      : null,
              "minArea"        : null,
              "maxArea"        : null,
              "floor"          : null,
              "minPrice"       : null,
              "maxPrice"       : 8000,
              "amenities"      : [],
              "confidence"     : 0.95,
              "detectedLanguage": "en"
            }
            """;

        var history = new ChatHistory();
        history.AddSystemMessage(
            "You are a precise real estate query parser. " +
            "Return only valid JSON with the exact structure requested.");
        history.AddUserMessage(prompt);

        var response = await _chat.GetChatMessageContentAsync(
            history,
            kernel: _kernel,
            cancellationToken: cancellationToken);

        var raw = response.Content ?? "{}";

        _logger.LogDebug("Claude parsing response: {Response}", raw);

        return ParseResponse(raw);
    }

    private ParsedSearchFilters ParseResponse(string raw)
    {
        try
        {
            var json = raw.Replace("```json", "").Replace("```", "").Trim();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string? GetString(string key) =>
                root.TryGetProperty(key, out var el) && el.ValueKind != JsonValueKind.Null
                    ? el.GetString() : null;

            int? GetInt(string key) =>
                root.TryGetProperty(key, out var el) && el.ValueKind != JsonValueKind.Null
                    ? el.GetInt32() : null;

            decimal? GetDecimal(string key) =>
                root.TryGetProperty(key, out var el) && el.ValueKind != JsonValueKind.Null
                    ? el.GetDecimal() : null;

            var amenities = root.TryGetProperty("amenities", out var amenEl)
                ? amenEl.EnumerateArray()
                         .Select(a => a.GetString() ?? string.Empty)
                         .Where(a => !string.IsNullOrEmpty(a))
                         .ToList()
                : new List<string>();

            var confidence = root.TryGetProperty("confidence", out var confEl)
                ? confEl.GetDecimal() : 0.5m;

            var language = root.TryGetProperty("detectedLanguage", out var langEl)
                ? langEl.GetString() ?? "en" : "en";

            var filters = new ParsedSearchFilters(
                PropertyType: GetString("propertyType"),
                City: GetString("city"),
                District: GetString("district"),
                NearbyLandmark: GetString("nearbyLandmark"),
                Bedrooms: GetInt("bedrooms"),
                Bathrooms: GetInt("bathrooms"),
                MinArea: GetInt("minArea"),
                MaxArea: GetInt("maxArea"),
                Floor: GetInt("floor"),
                MinPrice: GetDecimal("minPrice"),
                MaxPrice: GetDecimal("maxPrice"),
                Amenities: amenities,
                Confidence: confidence,
                DetectedLanguage: language);

            _logger.LogInformation(
                "Query parsed | Type={Type} City={City} District={District} " +
                "Beds={Beds} MaxPrice={Price} Amenities={Amenities} Lang={Lang} Conf={Conf}",
                filters.PropertyType, filters.City, filters.District,
                filters.Bedrooms, filters.MaxPrice,
                string.Join(",", filters.Amenities),
                filters.DetectedLanguage, filters.Confidence);

            return filters;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Claude response: {Raw}", raw);

            // Return empty filters — fallback to show all properties
            return new ParsedSearchFilters(
                PropertyType: null,
                City: null, District: null,
                NearbyLandmark: null, Bedrooms: null,
                Bathrooms: null, MinArea: null,
                MaxArea: null, Floor: null,
                MinPrice: null, MaxPrice: null,
                Amenities: [], Confidence: 0,
                DetectedLanguage: "en");
        }
    }
}