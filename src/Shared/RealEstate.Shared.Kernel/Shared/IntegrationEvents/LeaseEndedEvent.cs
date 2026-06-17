using MediatR;

namespace RealEstate.Shared.Kernel.Shared.IntegrationEvents;

/// <summary>
/// Published by the Document module when a lease ends — either terminated early or
/// swept after its EndDate passed. The Property module returns the listing to Active
/// and restores the owner's/company's property usage slot.
/// </summary>
public record LeaseEndedEvent(Guid LeaseId, Guid PropertyId) : INotification;
