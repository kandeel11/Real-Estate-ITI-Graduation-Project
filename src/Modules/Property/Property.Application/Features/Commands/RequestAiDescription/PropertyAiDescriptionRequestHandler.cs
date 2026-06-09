using MediatR;
using Property.Application.Interfaces;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;

namespace Property.Application.Features.Commands.RequestAiDescription;

public class PropertyAiDescriptionRequestHandler : IRequestHandler<PropertyAiDescriptionRequestCommand>
{
    private readonly IPropertyRepository _propertyRepository;
    private readonly IPublisher _publisher;

    public PropertyAiDescriptionRequestHandler(IPropertyRepository propertyRepository, IPublisher publisher)
    {
        _propertyRepository = propertyRepository;
        _publisher = publisher;
    }

    public async Task Handle(PropertyAiDescriptionRequestCommand request, CancellationToken cancellationToken)
    {
        var property = await _propertyRepository.GetByIdAsync(request.PropertyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Property {request.PropertyId} not found.");

        if (property.OwnerId != request.RequestingUserId)
            throw new UnauthorizedAccessException("Only the property owner can request an AI description.");

        // Allowed in any status, including Draft — owners can generate a description before submitting.

        await _publisher.Publish(new PropertyAiDescriptionRequestedEvent(
            PropertyId:   property.Id,
            OwnerId:      property.OwnerId,
            CompanyId:    property.CompanyId,
            Title:        property.Title,
            TitleAr:      property.TitleAr,
            PropertyType: property.PropertyType.ToString().ToLower(),
            Bedrooms:     property.Bedrooms,
            Bathrooms:    property.Bathrooms,
            AreaSqm:      property.AreaSqm,
            City:         property.City,
            District:     property.District,
            IsFurnished:  property.IsFurnished,
            PetsAllowed:  property.PetsAllowed,
            Price:        property.Price
        ), cancellationToken);
    }
}
