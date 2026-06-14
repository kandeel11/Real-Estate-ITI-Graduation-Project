using MediatR;
using Property.Application.Interfaces;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;

namespace Property.Application.Features.Commands.SubmitPropertyForReview;

public class SubmitForReviewHandler : IRequestHandler<SubmitPropertyForReviewCommand>
{
    private readonly IPropertyRepository _repository;
    private readonly IPublisher _publisher;

    public SubmitForReviewHandler(IPropertyRepository repository, IPublisher publisher)
    {
        _repository = repository;
        _publisher = publisher;
    }

    public async Task Handle(SubmitPropertyForReviewCommand request, CancellationToken cancellationToken)
    {
        var property = await _repository.GetByIdAsync(request.PropertyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Property {request.PropertyId} not found.");

        if (property.OwnerId != request.RequestingUserId)
            throw new UnauthorizedAccessException("Only the property owner can submit for review.");

        property.SubmitForReview();

        // Domain events are dispatched by DispatchDomainEventsInterceptor after save.
        await _repository.SaveChangesAsync(cancellationToken);

        // Publish integration event so AI module can run duplicate detection
        await _publisher.Publish(new PropertySubmittedForReviewEvent(
            PropertyId:      property.Id,
            OwnerId:         property.OwnerId,
            Title:           property.Title,
            Description:     property.Description,
            City:            property.City,
            District:        property.District,
            Address:         property.Address,
            ApartmentNumber: property.ApartmentNumber,
            FloorNumber:     property.FloorNumber,
            PropertyType:    property.PropertyType.ToString(),
            AreaSqm:         property.AreaSqm,
            Bedrooms:        property.Bedrooms,
            Bathrooms:       property.Bathrooms,
            Price:           property.Price
        ), cancellationToken);
    }
}
