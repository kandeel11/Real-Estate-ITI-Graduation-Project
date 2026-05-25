using Microsoft.SemanticKernel;
using Property.Application.Interfaces;
using System.ComponentModel;
using System.Text.Json;

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
        [Description("The city or location (e.g., Tanta, Cairo)")] string city,
        [Description("Number of bedrooms needed")] int? bedrooms,
        [Description("Type of property (e.g., Apartment, Villa)")] string? propertyType)
        {
            SearchCriteria criteria = new SearchCriteria
            {
                City = city,
                Bedrooms = bedrooms,
                PropertyType = propertyType
            };
            var properties = await _repository.SearchAsync(criteria);
            return JsonSerializer.Serialize(properties);

            //return $"Found 1 available {propertyType ?? "property"} in {city} with {bedrooms} bedrooms. Price is 5000 EGP/month.";
        }
    }
}
