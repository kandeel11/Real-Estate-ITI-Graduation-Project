using MediatR;
using Microsoft.Extensions.Logging;
using Property.Application.Interfaces;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;

namespace Property.Application.Features.EventHandlers;

public class PropertySubmittedAdminNotificationHandler
    : INotificationHandler<PropertySubmittedForReviewEvent>
{
    private readonly ILogger<PropertySubmittedAdminNotificationHandler> _logger;
    private readonly IAdminEmailService _adminEmail;

    public PropertySubmittedAdminNotificationHandler(
        ILogger<PropertySubmittedAdminNotificationHandler> logger,
        IAdminEmailService adminEmail)
    {
        _logger = logger;
        _adminEmail = adminEmail;
    }

    public async Task Handle(PropertySubmittedForReviewEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Property submitted for review | PropertyId: {PropertyId} | Title: '{Title}' | OwnerId: {OwnerId} | City: {City}",
            notification.PropertyId, notification.Title, notification.OwnerId, notification.City);

        try
        {
            await _adminEmail.NotifyPropertySubmittedAsync(
                notification.PropertyId, notification.Title, notification.OwnerId, notification.City, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send admin email for property {PropertyId}", notification.PropertyId);
        }
    }
}
