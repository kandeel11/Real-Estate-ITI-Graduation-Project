using MediatR;
using Property.Domain.Enums;

namespace Property.Application.Features.Commands.UpdateProperty;

public record UpdatePropertyCommand(
    Guid PropertyId,
    Guid RequestingUserId,
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
    int? ApartmentNumber,
    string City,
    string District,
    string ownerShipStatus, // e.g. "verified", "pending", "rejected"
    string governorate,
    Guid? ListingBrokerId
) : IRequest;
