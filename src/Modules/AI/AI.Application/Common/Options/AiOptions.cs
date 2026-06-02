namespace AI.Application.Common.Options
{
    public class AiOptions
    {
        //public const string SectionName = "AI";
        public const string SectionName = "AiOptions";
        public string AzureEmbeddingEndpoint { get; set; } = string.Empty;
        public string AzureEmbeddingApiKey { get; set; } = string.Empty;
        public string AzureEmbeddingDeploymentName { get; set; } = string.Empty;

        //public string OpenAiApiKey { get; set; } = string.Empty;
        //public string ModelVersion { get; set; } = "gpt-5-mini";

        //public string EmbeddingModelVersion { get; set; } = string.Empty;
      

        public string GroqApiKey { get; set; }
        public string GroqModelId { get; set; } = "llama-3.3-70b-versatile";
    }
}
