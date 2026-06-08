using MediatR;
using Microsoft.Extensions.Logging;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;

namespace Property.Application.Features.EventHandlers;

public class PropertySubmittedAdminNotificationHandler
    : INotificationHandler<PropertySubmittedForReviewEvent>
{
    private readonly ILogger<PropertySubmittedAdminNotificationHandler> _logger;

    public PropertySubmittedAdminNotificationHandler(ILogger<PropertySubmittedAdminNotificationHandler> logger)
        => _logger = logger;

    public Task Handle(PropertySubmittedForReviewEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "ADMIN NOTIFICATION | Property submitted for review | " +
            "PropertyId: {PropertyId} | Title: '{Title}' | OwnerId: {OwnerId} | " +
            "City: {City} | Type: {PropertyType} | Bedrooms: {Bedrooms} | Price: {Price} | " +
            "Review: GET /api/properties/pending-verification",
            notification.PropertyId,
            notification.Title,
            notification.OwnerId,
            notification.City,
            notification.PropertyType,
            notification.Bedrooms,
            notification.Price);

        return Task.CompletedTask;
    }
}
