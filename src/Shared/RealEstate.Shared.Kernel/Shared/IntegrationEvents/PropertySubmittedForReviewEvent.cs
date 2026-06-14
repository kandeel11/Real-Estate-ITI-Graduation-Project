using MediatR;

namespace RealEstate.Shared.Kernel.Shared.IntegrationEvents;

public record PropertySubmittedForReviewEvent(
    Guid PropertyId,
    Guid OwnerId,
    string Title,
    string Description,
    string City,
    string District,
    string Address,
    int? ApartmentNumber,
    int FloorNumber,
    string PropertyType,
    decimal AreaSqm,
    int Bedrooms,
    int Bathrooms,
    decimal Price
) : INotification;
