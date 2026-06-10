using MediatR;

namespace RealEstate.Shared.Kernel.Shared.IntegrationEvents;

public record PropertyDuplicateCheckCompletedEvent(
    Guid PropertyId,
    decimal SimilarityScore,    // how duplicate-like (0-1); high = very similar to an existing listing
    decimal DecisionConfidence, // how sure the engine/AI is in the verdict (0-1)
    bool IsDuplicate
) : INotification;
