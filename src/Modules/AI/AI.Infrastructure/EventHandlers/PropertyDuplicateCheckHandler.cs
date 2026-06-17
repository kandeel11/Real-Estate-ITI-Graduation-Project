using AI.Application.DTOs;
using AI.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;

namespace AI.Infrastructure.EventHandlers;

public sealed class PropertyDuplicateCheckHandler
    : INotificationHandler<PropertySubmittedForReviewEvent>
{
    private readonly IPropertyDuplicateCheckService _duplicateCheck;
    private readonly IPublisher _publisher;
    private readonly ILogger<PropertyDuplicateCheckHandler> _logger;

    public PropertyDuplicateCheckHandler(
        IPropertyDuplicateCheckService duplicateCheck,
        IPublisher publisher,
        ILogger<PropertyDuplicateCheckHandler> logger)
    {
        _duplicateCheck = duplicateCheck;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task Handle(
        PropertySubmittedForReviewEvent notification,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "AI received PropertySubmittedForReviewEvent | PropertyId={Id} | {Title}",
            notification.PropertyId, notification.Title);

        try
        {
            var result = await _duplicateCheck.CheckAsync(
                new PropertyDuplicateCheckRequest(
                    PropertyId:   notification.PropertyId,
                    OwnerId:      notification.OwnerId,
                    Title:        notification.Title,
                    Description:  notification.Description,
                    City:         notification.City,
                    District:     notification.District,
                    Street:       notification.Address,
                    BuildingName: null,
                    UnitNumber:   notification.ApartmentNumber?.ToString(),
                    Floor:        notification.FloorNumber,
                    PropertyType: notification.PropertyType,
                    ListingType:  "ForSale",
                    AreaInMeters: (double)notification.AreaSqm,
                    Bedrooms:     notification.Bedrooms,
                    Bathrooms:    notification.Bathrooms,
                    Price:        notification.Price),
                cancellationToken);

            await _publisher.Publish(new PropertyDuplicateCheckCompletedEvent(
                PropertyId:         notification.PropertyId,
                SimilarityScore:    (decimal)result.HighestSimilarityScore,
                DecisionConfidence: (decimal)result.DecisionConfidence,
                IsDuplicate:        result.IsDuplicate
            ), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Duplicate check pipeline failed for PropertyId={Id}", notification.PropertyId);
        }
    }
}
