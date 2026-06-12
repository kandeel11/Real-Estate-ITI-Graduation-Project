using AI.Application.DTOs;
using AI.Application.Interfaces.Common;
using AI.Infrastructure.Plugins;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
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
You are a professional AI assistant for a Real Estate and Property Management platform.

Your expertise is LIMITED STRICTLY to:
- Property search and recommendations
- Property details and availability
- Rental contracts and contract explanations
- Rent flows, payments, and lease-related questions
- Property maintenance requests and processes
- Property management operations

STRICT DOMAIN RESTRICTION:
You MUST ONLY answer questions related to real estate and property management.

If a user asks about anything outside this scope (for example: programming, coding, cooking, medicine, politics, sports, general knowledge, entertainment, personal advice, or unrelated topics), politely refuse and explain that you are exclusively a Real Estate and Property Management Assistant.

TOOL USAGE RULES:
- Use the 'SearchProperties' function whenever the user is looking for available properties, recommendations, listings, units, prices, locations, filters, or availability.
- Never invent property information, prices, locations, contracts, or availability.
- Only provide property information returned from platform tools or provided by the user.
- If required information is missing (budget, location, property type, bedrooms, etc.), ask concise follow-up questions.

COMMUNICATION STYLE:
- Be professional, concise, accurate, and helpful.
- Keep responses focused and actionable.
- Avoid unnecessary explanations.
- Speak in the SAME language used by the user (Arabic or English).
- If the user switches language, follow their language.

RESPONSE SAFETY:
- Do not make assumptions about contracts or legal obligations when information is missing.
- If contract details are unavailable, clearly say so.
- Never pretend to access data that was not returned by the system.

EXAMPLES OF OUT-OF-SCOPE REQUESTS:
- 'Write C# code'
- 'Who won the World Cup?'
- 'Teach me Python'
- 'Best restaurant nearby'
Politely refuse such requests.
";
        public async Task<(string EnDescription, string ArDescription, int TokensUsed)> GenerateDescriptionAsync(string propertyType, string location, int bedrooms, decimal area, List<string> amenities, CancellationToken cancellationToken)
        {
            var amenitiesStr = string.Join(", ", amenities);

            // System Prompt + User Data
            
var prompt = $@"
You are an expert Real Estate Copywriter and SEO Content Specialist.

Your task is to generate HIGH-QUALITY, SEO-OPTIMIZED property descriptions using ONLY the provided facts.

PROPERTY DETAILS:
- Property Type: {propertyType}
- Location: {location}
- Bedrooms: {bedrooms}
- Area: {area} sqm
- Amenities: {amenitiesStr}

OBJECTIVE:
Write clear, engaging, professional, and search-friendly property descriptions in BOTH English and Arabic.

STRICT FACTUAL RULES:
1. Use ONLY the information explicitly provided in PROPERTY DETAILS.
2. NEVER invent, assume, infer, or exaggerate:
   - finishes or interior quality
   - nearby landmarks, schools, transport, malls, hospitals, or attractions
   - views, lifestyle benefits, compound features, security, parking, garden, pool, gym, etc.
   - investment value or ROI claims
3. DO NOT mention whether the property is for sale or rent.
4. If a field is missing or empty, ignore it naturally and do not mention it.
5. Do not repeat the same information unnecessarily.

SEO RULES:
1. Naturally include available keywords such as:
   - property type
   - location
   - bedrooms
   - area
   - amenities
2. Keep SEO natural and human-friendly.
3. Avoid keyword stuffing.
4. The description should be readable and suitable for property listing pages.

WRITING STYLE:
- Professional, concise, and inviting.
- Avoid exaggerated marketing language.
- Sound trustworthy and premium without making unsupported claims.
- Maximum 120 words per language.

OUTPUT REQUIREMENTS:
- Return ONLY a valid JSON object.
- No markdown.
- No explanation.
- No extra text.
- Escape quotes properly for JSON validity.

REQUIRED JSON FORMAT:
{{
  ""en"": ""English property description"",
  ""ar"": ""الوصف العربي للعقار""
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

    // 1. أخد نسخة من الكيرنل الأساسي
    var chatKernel = _kernel.Clone();
            var contractPlugin = chatKernel.Plugins
     .FirstOrDefault(p => p.Name == "ContractRetrievalPlugin");

            if (contractPlugin != null)
            {
                chatKernel.Plugins.Remove(contractPlugin);
            }
            // 2. نمسح البلوجن بتاع العقود من النسخة دي عشان الـ AI مايشوفوش خالص
            //chatKernel.Plugins.Remove("ContractRetrievalPlugin");

            // 3. نرجع نستخدم الـ AutoInvoke عادي جداً، لأنه دلوقتي مش شايف غير العقارات
            var executionSettings = new OpenAIPromptExecutionSettings
    {
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
        Temperature = 0.3
    };

    var chatCompletionService = chatKernel.GetRequiredService<IChatCompletionService>();

    _logger.LogInformation("Sending request to AI with {MessageCount} messages.", chatHistory.Count);
    
    // 4. نمرر الـ chatKernel المفلتر بدل الـ _kernel الأساسي
    var response = await chatCompletionService.GetChatMessageContentAsync(
        chatHistory,
        executionSettings,
        kernel: chatKernel, 
        cancellationToken: cancellationToken);

    int tokensUsed = 0;
    if (response.Metadata != null && response.Metadata.TryGetValue("Usage", out var usage) && usage != null)
    {
        try
        {
            var totalTokensProp = usage.GetType().GetProperty("TotalTokens");
            if (totalTokensProp != null) tokensUsed = (int)totalTokensProp.GetValue(usage)!;
        }
        catch 
        {
            _logger.LogWarning("Failed to extract token usage from AI response metadata.");
        }
    }
    
    chatHistory.AddAssistantMessage(response.Content ?? "");

    return new ChatbotAiResponse(response.Content ?? "Sorry, an unexpected error occurred.", tokensUsed);
}
    }
}

