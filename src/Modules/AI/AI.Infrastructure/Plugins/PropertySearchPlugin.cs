using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace AI.Infrastructure.Plugins
{
    public class PropertySearchPlugin
    {

        [KernelFunction("SearchProperties")]
        [Description("Searches for available properties based on user requirements like city, bedrooms, and property type.")]
        public async Task<string> SearchPropertiesAsync(
        [Description("The city or location (e.g., Tanta, Cairo)")] string city,
        [Description("Number of bedrooms needed")] int? bedrooms,
        [Description("Type of property (e.g., Apartment, Villa)")] string? propertyType)
        {

            // var properties = await _repository.SearchAsync(city, bedrooms, propertyType);
            // return JsonSerializer.Serialize(properties);

            return $"Found 1 available {propertyType ?? "property"} in {city} with {bedrooms} bedrooms. Price is 5000 EGP/month.";
        }
    }
}
