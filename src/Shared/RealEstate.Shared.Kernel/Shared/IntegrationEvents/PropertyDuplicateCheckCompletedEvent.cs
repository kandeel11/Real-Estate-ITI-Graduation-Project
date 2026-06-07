using MediatR;

namespace RealEstate.Shared.Kernel.Shared.IntegrationEvents;

public record PropertyDuplicateCheckCompletedEvent(
    Guid PropertyId,
    decimal Confidence,
    bool IsDuplicate
) : INotification;
