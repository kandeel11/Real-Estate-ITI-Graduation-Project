using MediatR;

namespace RealEstate.Shared.Kernel.Shared.IntegrationEvents;

public record PropertyAiDescriptionRequestedEvent(
    Guid PropertyId,
    Guid OwnerId,
    Guid? CompanyId,
    string Title,
    string TitleAr,
    string PropertyType,
    int Bedrooms,
    int Bathrooms,
    decimal AreaSqm,
    string City,
    string District,
    bool IsFurnished,
    bool PetsAllowed,
    decimal Price
) : INotification;
