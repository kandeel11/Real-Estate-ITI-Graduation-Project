using MediatR;
using Microsoft.Extensions.Logging;
using Property.Application.Features.Commands.SetAiDuplicateScore;
using Property.Application.Features.Commands.SetPropertyGate;
using Property.Domain.Enums;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;

namespace Property.Application.Features.EventHandlers;

public class PropertyDuplicateCheckCompletedHandler
    : INotificationHandler<PropertyDuplicateCheckCompletedEvent>
{
    // Auto-act only when the engine/AI is at least this sure of its verdict.
    private const decimal AutoDecisionThreshold = 0.80m;
    // ...and only when similarity is decisive: very high → duplicate, very low → unique.
    private const decimal HighSimilarityThreshold = 0.80m; // > 80% similar → auto-reject
    private const decimal LowSimilarityThreshold = 0.20m;  // < 20% similar → auto-verify

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
        // Always record the similarity score on the property (visible to admins).
        await _mediator.Send(
            new SetAiDuplicateScoreCommand(notification.PropertyId, notification.SimilarityScore),
            cancellationToken);

        var scores = $"similarity {notification.SimilarityScore:P0}, AI decision confidence {notification.DecisionConfidence:P0}";

        var confident = notification.DecisionConfidence > AutoDecisionThreshold;

        GateStatus gate;
        string? note;
        if (confident && notification.SimilarityScore > HighSimilarityThreshold)
        {
            // Confident AND very similar → duplicate gate FAILS (auto-reject).
            gate = GateStatus.Failed;
            note = $"Auto-rejected — likely duplicate ({scores}). An admin can override if it's a distinct listing.";
            _logger.LogWarning("Property {PropertyId} duplicate gate FAILED ({Scores}).", notification.PropertyId, scores);
        }
        else if (confident && notification.SimilarityScore < LowSimilarityThreshold)
        {
            // Confident AND clearly unique → duplicate gate PASSES.
            gate = GateStatus.Passed;
            note = null;
            _logger.LogInformation("Property {PropertyId} duplicate gate PASSED ({Scores}).", notification.PropertyId, scores);
        }
        else
        {
            // Low confidence, OR similarity in the ambiguous 20%–80% band → human decides.
            gate = GateStatus.NeedsReview;
            note = $"Duplicate check inconclusive — needs manual review ({scores}).";
            _logger.LogInformation("Property {PropertyId} duplicate gate → NEEDS REVIEW ({Scores}).", notification.PropertyId, scores);
        }

        await _mediator.Send(
            new SetPropertyGateCommand(notification.PropertyId, PropertyGateType.Duplicate, gate, note),
            cancellationToken);
    }
}
