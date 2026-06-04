using MediatR;
using Property.Application.Common;
using Property.Application.DTOs;
using Property.Application.Interfaces;

namespace Property.Application.Features.Queries.SearchProperties;

public class SearchPropertiesHandler : IRequestHandler<SearchPropertiesQuery, PagedResult<PropertySummaryDto>>
{
    private readonly IPropertyRepository _repository;

    public SearchPropertiesHandler(IPropertyRepository repository)
    {
        _repository = repository;
    }

    public Task<PagedResult<PropertySummaryDto>> Handle(SearchPropertiesQuery request, CancellationToken cancellationToken)
    {
        var criteria = new SearchCriteria
        {
            City = request.City,
            District = request.District,
            PropertyType = request.PropertyType,
            MinPrice = request.MinPrice,
            MaxPrice = request.MaxPrice,
            MinBedrooms = request.MinBedrooms,
            MaxBedrooms = request.MaxBedrooms,
            Bedrooms = request.Bedrooms,
            IsFurnished = request.IsFurnished,
            PetsAllowed = request.PetsAllowed,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            RadiusKm = request.RadiusKm,
            Page = request.Page,
            PageSize = request.PageSize
        };

        return _repository.SearchAsync(criteria, cancellationToken);
    }
}
