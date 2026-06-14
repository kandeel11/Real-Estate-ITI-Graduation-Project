using MediatR;
using Microsoft.Extensions.Logging;
using Property.Application.Features.Commands.SetPropertyGate;
using Property.Domain.Enums;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;

namespace Property.Application.Features.EventHandlers;

/// <summary>
/// Sets the property's OWNERSHIP gate from the AI ownership analysis.
/// The property only goes Active once the duplicate gate also clears.
///
/// AUTO_VERIFY → Passed | MANUAL_REVIEW → NeedsReview | REJECT → Failed
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
        var gate = MapRecommendation(notification.Recommendation);

        _logger.LogInformation(
            "Ownership gate | PropertyId={PropertyId} Recommendation={Rec} → {Gate} (confidence={Score})",
            notification.PropertyId, notification.Recommendation, gate, notification.ConfidenceScore);

        var note = gate switch
        {
            GateStatus.Failed => notification.AnalysisNotes ?? "Ownership documents rejected by automated verification.",
            GateStatus.NeedsReview => notification.AnalysisNotes ?? "Ownership documents need manual review.",
            _ => null
        };

        await _mediator.Send(
            new SetPropertyGateCommand(notification.PropertyId, PropertyGateType.Ownership, gate, note),
            cancellationToken);
    }

    private static GateStatus MapRecommendation(string recommendation)
        => recommendation?.ToUpperInvariant() switch
        {
            "AUTO_VERIFY" => GateStatus.Passed,
            "REJECT" => GateStatus.Failed,
            "MANUAL_REVIEW" => GateStatus.NeedsReview,
            _ => GateStatus.NeedsReview   // unknown verdict → safest is human review
        };
}
