// AI.Infrastructure/Services/GeminiVisionClient.cs
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AI.Infrastructure.Services;

/// <summary>
/// Thin wrapper around Gemini's multimodal generateContent endpoint — sends a
/// document/image alongside a prompt and asks for a JSON-shaped answer.
/// Shared by the lease-OCR and ownership-verification extractors, which each
/// own their own prompt and result DTO.
/// </summary>
internal sealed class GeminiVisionClient
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _model;

    public GeminiVisionClient(HttpClient http, string apiKey, string model)
    {
        _http = http;
        _apiKey = apiKey;
        _model = model;
    }

    public async Task<string> GenerateJsonAsync(
        string prompt, byte[] fileBytes, string mimeType, CancellationToken ct)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

        var request = new GenerateContentRequest
        {
            Contents =
            [
                new GeminiContent
                {
                    Parts =
                    [
                        new GeminiPart { Text = prompt },
                        new GeminiPart
                        {
                            InlineData = new GeminiInlineData
                            {
                                MimeType = mimeType,
                                Data = Convert.ToBase64String(fileBytes)
                            }
                        }
                    ]
                }
            ],
            GenerationConfig = new GeminiGenerationConfig { ResponseMimeType = "application/json" }
        };

        using var response = await _http.PostAsJsonAsync(url, request, ct);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        return doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString()
            ?? throw new InvalidOperationException("Gemini returned no text content.");
    }

    private sealed class GenerateContentRequest
    {
        [JsonPropertyName("contents")]
        public List<GeminiContent> Contents { get; set; } = [];

        [JsonPropertyName("generationConfig")]
        public GeminiGenerationConfig GenerationConfig { get; set; } = new();
    }

    private sealed class GeminiContent
    {
        [JsonPropertyName("parts")]
        public List<GeminiPart> Parts { get; set; } = [];
    }

    private sealed class GeminiPart
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("inline_data")]
        public GeminiInlineData? InlineData { get; set; }
    }

    private sealed class GeminiInlineData
    {
        [JsonPropertyName("mime_type")]
        public string MimeType { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public string Data { get; set; } = string.Empty;
    }

    private sealed class GeminiGenerationConfig
    {
        [JsonPropertyName("responseMimeType")]
        public string ResponseMimeType { get; set; } = "application/json";
    }
}
