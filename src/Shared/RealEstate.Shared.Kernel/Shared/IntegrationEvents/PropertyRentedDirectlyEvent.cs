using MediatR;

namespace RealEstate.Shared.Kernel.Shared.IntegrationEvents;

/// <summary>
/// Published when a deal closes without a lease (independent broker or company broker
/// choosing no management). Signals the Property module to mark the property as Rented
/// without creating a payment schedule.
/// </summary>
public record PropertyRentedDirectlyEvent(
    Guid PropertyId,
    Guid OwnerId
) : INotification;
