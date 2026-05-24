using AI.Application.DTOs;
using AI.Application.Interfaces.Common;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text.Json;

namespace AI.Infrastructure.Services
{
    public class SemanticKernelService : ISemanticKernelService
    {
        private readonly Kernel _kernel;
        private readonly ILogger<SemanticKernelService> _logger;
        public SemanticKernelService(Kernel kernel, ILogger<SemanticKernelService> logger)
        {
            _kernel = kernel;
            _logger = logger;
        }
        private const string SystemPrompt = @"
You are an expert Real Estate and Property Management Assistant for our platform. 
Your ONLY job is to help users search for properties, understand their contracts, rent flows, and maintenance processes.

CRITICAL RULES:
1. IF the user asks about ANYTHING outside real estate (e.g., coding, cooking, general knowledge, politics), you MUST politely refuse and state that you are exclusively a real estate assistant.
2. Use the 'SearchProperties' function when the user asks for available properties.
3. Be professional, concise, and helpful.
4. Speak in the same language the user uses (Arabic or English).";
        public async Task<(string EnDescription, string ArDescription, int TokensUsed)> GenerateDescriptionAsync(string propertyType, string location, int bedrooms, decimal area, List<string> amenities, CancellationToken cancellationToken)
        {
            var amenitiesStr = string.Join(", ", amenities);

            // System Prompt + User Data
            var prompt = $@"
You are an expert Real Estate Copywriter. Your task is to write highly engaging property descriptions based ONLY on the provided facts.

Property Details:
- Type: {propertyType}
- Location: {location}
- Bedrooms: {bedrooms}
- Area: {area} sqm
- Amenities: {amenitiesStr}

CRITICAL RULES:
1. DO NOT invent, assume, or add any features, amenities, nearby locations (like schools, shops, transport), or finishes that are not explicitly listed in the Property Details.
2. DO NOT state whether the property is for rent or for sale, as this information is not provided.
3. Your tone should be professional and inviting, but strictly limited to the factual data provided.
4. You MUST respond ONLY with a valid JSON object in the exact format below.

JSON FORMAT:
{{
  ""en"": ""English description here..."",
  ""ar"": ""Arabic description here...""
}}";

            //  Semantic Kernel
            var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
            var resultText = result.GetValue<string>() ?? "{}";

            // Markdown Tags 
            resultText = resultText.Replace("```json", "").Replace("```", "").Trim();

            try
            {
                // تحويل الـ JSON إلى C# Object
                using var doc = JsonDocument.Parse(resultText);
                var enDesc = doc.RootElement.GetProperty("en").GetString() ?? "";
                var arDesc = doc.RootElement.GetProperty("ar").GetString() ?? "";

                int estimatedTokens = (prompt.Length + resultText.Length) / 4;

                return (enDesc, arDesc, estimatedTokens);
            }
            catch (JsonException)
            {
                throw new Exception("AI generated an invalid response format.");
            }
        }

        public async Task<ChatbotAiResponse> SendChatMessageAsync(ChatHistory chatHistory, CancellationToken cancellationToken)
        {
            if (chatHistory.Count == 0 || chatHistory[0].Role != AuthorRole.System)
            {
                chatHistory.Insert(0, new ChatMessageContent(AuthorRole.System, SystemPrompt));
            }
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                //Temperature = 0.3

            };
            var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();

            _logger.LogInformation("Sending request to AI with {MessageCount} messages.", chatHistory.Count);
            var response = await chatCompletionService.GetChatMessageContentAsync(
            chatHistory,
            executionSettings,
            kernel: _kernel,
            cancellationToken: cancellationToken);
            int tokensUsed = 0;
            if (response.Metadata != null && response.Metadata.TryGetValue("Usage", out var usage) && usage != null)
            {
                try
                {
                    var totalTokensProp = usage.GetType().GetProperty("TotalTokens");
                    if (totalTokensProp != null) tokensUsed = (int)totalTokensProp.GetValue(usage)!;
                }
                catch {

                    _logger.LogWarning("Failed to extract token usage from AI response metadata.");
                }
            }
            chatHistory.AddAssistantMessage(response.Content ?? "");

            return new ChatbotAiResponse(response.Content ?? "Sorry, an unexpected error occurred.", tokensUsed);

        }
    }
}

