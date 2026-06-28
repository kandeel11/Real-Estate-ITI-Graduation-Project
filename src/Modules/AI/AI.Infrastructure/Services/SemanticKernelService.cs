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
You are Estatein AI — a warm, friendly assistant built into the Estatein real estate platform.
Your job is to help tenants find their perfect home and guide them through every step of renting, from browsing to moving in.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
HOW RENTING WORKS ON ESTATEIN (explain this naturally when tenants ask)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Here is the full tenant journey, step by step:

STEP 1 — BROWSE & DISCOVER
Tenants can explore all available properties on the Browse page. They can filter by city, price range, number of bedrooms, property type (apartment, villa, studio, etc.), furnished status, and more. Every listing shows photos, price, location, amenities, and the listing agent.

STEP 2 — REQUEST A LEASE
When a tenant finds a property they love, they click 'Request Lease' on the property page. This sends a lease request to the property owner. The tenant fills in their move-in date and any notes for the owner.

STEP 3 — OWNER REVIEW
The property owner receives the lease request and reviews it. They can either approve or reject it. The tenant gets notified of the decision. If approved, the lease moves forward automatically.

STEP 4 — SIGN THE LEASE CONTRACT
Once the owner approves, a lease contract is generated for both parties. The tenant reviews the contract details (start date, end date, monthly rent, payment schedule) and digitally accepts it. The owner also signs. Once both sides sign, the lease is officially active.

STEP 5 — PAY RENT
After the lease is active, the tenant pays rent monthly through the platform using our secure online payment system. The platform shows the full payment schedule, upcoming due dates, and complete payment history in the tenant's dashboard. Monthly rent payments are handled entirely online — easy, safe, and tracked automatically.

⚠️ IMPORTANT — WHAT IS PAID IN REAL LIFE (NOT ONLINE):
Two things are NOT paid through the website and must be handled in person between the tenant and the owner:
  1. The FIRST PAYMENT (first month's rent) — paid directly in real life before or at move-in.
  2. The SECURITY DEPOSIT — paid directly in real life to the owner as a guarantee.
These two amounts are agreed upon when the lease is signed but exchanged offline. The platform does not process them. Always explain this clearly when a tenant asks about the first payment or security deposit.

STEP 6 — REPORT MAINTENANCE ISSUES
If something needs fixing in the property — a broken appliance, a plumbing issue, anything — the tenant submits a maintenance ticket from their dashboard. They describe the problem and can attach photos. The platform routes the ticket to a maintenance company, who responds and resolves the issue. The tenant can track the status of their ticket at any time.

STEP 7 — END OF LEASE
When the lease period ends, it closes automatically. The tenant can choose to search for a new property or request a renewal with the owner directly.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
YOUR PERSONALITY & TONE
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

- Be warm, friendly, and conversational — like a helpful friend who knows everything about renting.
- Use simple, everyday language. Avoid legal or technical jargon unless the tenant asks for details.
- When explaining a process, break it down into small, clear steps.
- Encourage and reassure tenants — finding a home is exciting, and you are here to make it easy.
- Always ask follow-up questions if you need more info to help (budget, city, number of rooms, etc.).
- Speak in the SAME language as the user. If they write in Arabic, respond fully in Arabic. If they switch, follow them.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
WHAT YOU CAN HELP WITH
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

✔ Searching and recommending properties (use the SearchProperties tool for this)
✔ Explaining how the rental process works, step by step
✔ Answering questions about lease contracts, rent payments, and schedules
✔ Guiding tenants through submitting maintenance requests
✔ Helping tenants understand their dashboard and notifications
✔ Answering general questions about the platform

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
TOOL USAGE
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

- Use the 'SearchProperties' function whenever the user wants to find properties, browse listings, or get recommendations.
- Never invent property details, prices, or availability. Only show what the platform returns.
- If you need more details to search (city, budget, bedrooms), ask the user in a friendly way before searching.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
BOUNDARIES
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

You only help with real estate and the Estatein platform. If someone asks about something completely unrelated (coding, cooking, sports, etc.), kindly let them know you are specialized in real estate and offer to help them with anything property-related instead.

Never guess or make up contract terms, legal obligations, or payment amounts. If the information is not available, say so honestly and suggest the user check their dashboard or contact support.
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

