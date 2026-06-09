using MediatR;
using Microsoft.Extensions.Logging;
using Property.Application.Features.Commands.SetVerificationStatus;
using Property.Domain.Enums;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;

namespace Property.Application.Features.EventHandlers;

/// <summary>
/// Closes the owner verification loop: when the AI module finishes analyzing the
/// ownership documents, this flips the property's VerificationStatus accordingly.
///
/// AUTO_VERIFY → AiVerified (property becomes Active)
/// MANUAL_REVIEW → RequiresManualReview (waits for admin approve/reject)
/// REJECT → Rejected
/// </summary>
public class OwnershipVerificationCompletedHandler
    : INotificationHandler<OwnershipVerificationCompletedEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<OwnershipVerificationCompletedHandler> _logger;

    public OwnershipVerificationCompletedHandler(
        IMediator mediator,
        ILogger<OwnershipVerificationCompletedHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(OwnershipVerificationCompletedEvent notification, CancellationToken cancellationToken)
    {
        var status = MapRecommendation(notification.Recommendation, notification.Status);

        _logger.LogInformation(
            "Ownership verification completed | PropertyId={PropertyId} Recommendation={Rec} → VerificationStatus={Status} (confidence={Score})",
            notification.PropertyId, notification.Recommendation, status, notification.ConfidenceScore);

        var rejectionReason = status == VerificationStatus.Rejected
            ? notification.AnalysisNotes ?? "Ownership documents rejected by automated verification."
            : null;

        await _mediator.Send(
            new SetVerificationStatusCommand(notification.PropertyId, status, rejectionReason),
            cancellationToken);
    }

    private static VerificationStatus MapRecommendation(string recommendation, string status)
        => recommendation?.ToUpperInvariant() switch
        {
            "AUTO_VERIFY" => VerificationStatus.AiVerified,
            "REJECT" => VerificationStatus.Rejected,
            "MANUAL_REVIEW" => VerificationStatus.RequiresManualReview,
            // Fall back to the Status string if Recommendation is unrecognized
            _ => Enum.TryParse<VerificationStatus>(status, ignoreCase: true, out var parsed)
                ? parsed
                : VerificationStatus.RequiresManualReview
        };
}
