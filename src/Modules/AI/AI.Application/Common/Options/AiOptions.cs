namespace AI.Application.Common.Options
{
    public class AiOptions
    {
        public const string SectionName = "AI";

        // ── Azure OpenAI Embeddings (quota/subscription ended — kept for reference) ──
        //public string AzureEmbeddingEndpoint { get; set; } = string.Empty;
        //public string AzureEmbeddingApiKey { get; set; } = string.Empty;
        //public string AzureEmbeddingDeploymentName { get; set; } = string.Empty;

        // ── Gemini Embeddings (replaces Azure OpenAI embeddings) ──────────────
        public string GeminiApiKey { get; set; } = string.Empty;
        public string GeminiEmbeddingModel { get; set; } = "text-embedding-004";

        // ── Gemini Vision (replaces Azure Document Intelligence OCR) ─────────
        // Model names churn fast — bump this in config if it gets retired.
        public string GeminiVisionModel { get; set; } = "gemini-2.0-flash";

        // ── Groq Chat Model ───────────────────────────────────────────────────
        public string GroqApiKey { get; set; } = string.Empty;
        public string GroqModelId { get; set; } = "llama-3.3-70b-versatile";

        // ── OpenAI (quota ended — kept for reference, do not delete) ─────────
        //public string OpenAiApiKey { get; set; } = string.Empty;
        //public string ModelVersion { get; set; } = "gpt-4o-mini";
        //public string EmbeddingModelVersion { get; set; } = string.Empty; // was "text-embedding-3-small"
    }
}
