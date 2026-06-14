using MediatR;
using Property.Application.Common;
using Property.Application.DTOs;

namespace Property.Application.Features.Queries.SearchProperties;

public record SearchPropertiesQuery(
    string? City,
    string? District,
    string? PropertyType,
    decimal? MinPrice,
    decimal? MaxPrice,
    int? MinBedrooms,
    int? MaxBedrooms,
    int? Bedrooms,
    bool? IsFurnished,
    bool? PetsAllowed,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<PropertySummaryDto>>;
