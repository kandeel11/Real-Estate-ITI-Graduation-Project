// AI.Infrastructure/Services/GeminiTextEmbeddingGenerationService.cs
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;

namespace AI.Infrastructure.Services;

/// <summary>
/// Text embeddings via Google's Gemini API (text-embedding-004), replacing
/// Azure OpenAI embeddings after the Azure subscription was disabled.
/// Implements the (obsolete but still-consumed) Semantic Kernel embedding
/// interface so no downstream consumer needs to change.
/// </summary>
#pragma warning disable SKEXP0001
public sealed class GeminiTextEmbeddingGenerationService : ITextEmbeddingGenerationService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _model;

    public IReadOnlyDictionary<string, object?> Attributes { get; } =
        new Dictionary<string, object?>();

    public GeminiTextEmbeddingGenerationService(HttpClient http, string apiKey, string model)
    {
        _http = http;
        _apiKey = apiKey;
        _model = model;
    }

    public async Task<IList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(
        IList<string> data,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            return data.Select(_ => ReadOnlyMemory<float>.Empty).ToList();

        var request = new BatchEmbedRequest
        {
            Requests = data.Select(text => new EmbedContentRequest
            {
                Model = $"models/{_model}",
                Content = new GeminiContent
                {
                    Parts = [new GeminiPart { Text = text }]
                }
            }).ToList()
        };

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:batchEmbedContents?key={_apiKey}";
        using var response = await _http.PostAsJsonAsync(url, request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<BatchEmbedResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Gemini embeddings API returned an empty response.");

        return result.Embeddings
            .Select(e => new ReadOnlyMemory<float>(e.Values.ToArray()))
            .ToList();
    }

    private sealed class BatchEmbedRequest
    {
        [JsonPropertyName("requests")]
        public List<EmbedContentRequest> Requests { get; set; } = [];
    }

    private sealed class EmbedContentRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public GeminiContent Content { get; set; } = new();
    }

    private sealed class GeminiContent
    {
        [JsonPropertyName("parts")]
        public List<GeminiPart> Parts { get; set; } = [];
    }

    private sealed class GeminiPart
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    private sealed class BatchEmbedResponse
    {
        [JsonPropertyName("embeddings")]
        public List<GeminiEmbedding> Embeddings { get; set; } = [];
    }

    private sealed class GeminiEmbedding
    {
        [JsonPropertyName("values")]
        public List<float> Values { get; set; } = [];
    }
}
#pragma warning restore SKEXP0001
