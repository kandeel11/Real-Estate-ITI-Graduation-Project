using MediatR;
using Property.Domain.Enums;

namespace Property.Application.Features.Commands.CreateProperty;

public record CreatePropertyCommand(
    Guid OwnerId,
    Guid? CompanyId,
    Guid? ListingBrokerId,
    string Title,
    string TitleAr,
    string Description,
    string DescriptionAr,
    PropertyType PropertyType,
    decimal Price,
    int Bedrooms,
    int Bathrooms,
    decimal AreaSqm,
    int FloorNumber,
    int TotalFloors,
    int YearBuilt,
    bool IsFurnished,
    bool PetsAllowed,
    decimal Latitude,
    decimal Longitude,
    string Address,
    string City,
    string District,
    ListingSource ListingSource
) : IRequest<Guid>;
