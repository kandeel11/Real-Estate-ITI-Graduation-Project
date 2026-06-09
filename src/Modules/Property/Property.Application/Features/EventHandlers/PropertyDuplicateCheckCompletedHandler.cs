using MediatR;
using Microsoft.Extensions.Logging;
using Property.Application.Features.Commands.SetAiDuplicateScore;
using Property.Application.Features.Commands.SetVerificationStatus;
using Property.Domain.Enums;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;

namespace Property.Application.Features.EventHandlers;

public class PropertyDuplicateCheckCompletedHandler
    : INotificationHandler<PropertyDuplicateCheckCompletedEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<PropertyDuplicateCheckCompletedHandler> _logger;

    public PropertyDuplicateCheckCompletedHandler(
        IMediator mediator,
        ILogger<PropertyDuplicateCheckCompletedHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(PropertyDuplicateCheckCompletedEvent notification, CancellationToken cancellationToken)
    {
        // Always record the AI duplicate-confidence score for admin visibility.
        await _mediator.Send(
            new SetAiDuplicateScoreCommand(notification.PropertyId, notification.Confidence),
            cancellationToken);

        // Confident duplicates are auto-rejected. The human stays the safety net:
        // an admin can override a false positive via PUT /verification (ManuallyVerified),
        // which flips the property back to Active.
        if (notification.IsDuplicate)
        {
            _logger.LogWarning(
                "Property {PropertyId} auto-rejected as a likely duplicate (confidence={Score}). An admin can override.",
                notification.PropertyId, notification.Confidence);

            await _mediator.Send(
                new SetVerificationStatusCommand(
                    notification.PropertyId,
                    VerificationStatus.Rejected,
                    $"Auto-rejected: flagged as a likely duplicate listing (confidence {notification.Confidence:P0}). " +
                    "An admin can override this if it is a genuinely distinct listing."),
                cancellationToken);
        }
    }
}
