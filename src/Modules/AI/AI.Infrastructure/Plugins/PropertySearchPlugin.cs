using Microsoft.SemanticKernel;
using Property.Application.Interfaces;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization; // عشان الـ ReferenceHandler

namespace AI.Infrastructure.Plugins
{
    public class PropertySearchPlugin
    {
        private readonly IPropertyRepository _repository;

        public PropertySearchPlugin(IPropertyRepository repository)
        {
            _repository = repository;
        }

        [KernelFunction("SearchProperties")]
        [Description("Searches for available properties based on user requirements like city, bedrooms, and property type.")]
        public async Task<string> SearchPropertiesAsync(
            [Description("The city to search in. CRITICAL: If the user speaks Arabic, ALWAYS translate the city name to its English equivalent before passing it here (e.g., translate 'القاهرة الجديدة' to 'New Cairo').")]
            string? city = null, // ✅ خلينا المدينة Optional

            [Description("Number of bedrooms needed")]
            int? bedrooms = null,

            [Description("Property type. MUST be in English (e.g., 'Apartment', 'Villa'). Translate from Arabic if necessary.")]
            string? propertyType = null)
        {
            var criteria = new SearchCriteria
            {
                City = city,
                Bedrooms = bedrooms,
                PropertyType = propertyType
            };

            var properties = await _repository.SearchAsync(criteria);

            // ✅ حماية الـ Tokens: ناخد أفضل 5 نتائج بس
            var topResults = properties.Items.Take(5).ToList();

            // ✅ حماية السرياليزيشن من الـ Entity Framework Loops
            var jsonOptions = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                WriteIndented = false // خليه false عشان نوفر Tokens في المسافات
            };

            return JsonSerializer.Serialize(topResults, jsonOptions);
        }
    }
}