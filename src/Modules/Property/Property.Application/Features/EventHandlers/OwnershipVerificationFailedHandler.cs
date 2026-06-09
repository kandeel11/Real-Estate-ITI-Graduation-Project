using MediatR;
using Microsoft.Extensions.Logging;
using Property.Application.Features.Commands.SetVerificationStatus;
using Property.Domain.Enums;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;

namespace Property.Application.Features.EventHandlers;

/// <summary>
/// Fallback when AI ownership analysis fails (timeout, bad API key, OCR/download error).
///
/// We do NOT reject the owner over a transient infrastructure failure, and we do NOT
/// auto-verify unchecked documents. Instead the property is routed to RequiresManualReview
/// so an admin can verify it by hand. This is graceful degradation — a legitimate listing
/// is never left stuck in Pending because of an AI outage.
/// </summary>
public class OwnershipVerificationFailedHandler
    : INotificationHandler<OwnershipVerificationFailedEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<OwnershipVerificationFailedHandler> _logger;

    public OwnershipVerificationFailedHandler(
        IMediator mediator,
        ILogger<OwnershipVerificationFailedHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(OwnershipVerificationFailedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Ownership verification FAILED for PropertyId={PropertyId} (Reason={Reason}) — routing to manual review",
            notification.PropertyId, notification.Reason);

        await _mediator.Send(
            new SetVerificationStatusCommand(
                notification.PropertyId,
                VerificationStatus.RequiresManualReview,
                RejectionReason: null),
            cancellationToken);
    }
}
