using AI.Application.Interfaces.Common;
using Microsoft.SemanticKernel;
using System.Text.Json;

namespace AI.Infrastructure.Services
{
    public class SemanticKernelService : ISemanticKernelService
    {
        private readonly Kernel _kernel;
        public SemanticKernelService(Kernel kernel)
        {
            _kernel = kernel;
        }
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

    }
}

