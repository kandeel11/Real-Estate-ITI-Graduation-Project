namespace AI.Application.Common.Options
{
    public class AiOptions
    {
        public const string SectionName = "AI";

        public string OpenAiApiKey { get; set; } = string.Empty;
        public string ModelVersion { get; set; } = "gpt-5-mini";
    }
}
