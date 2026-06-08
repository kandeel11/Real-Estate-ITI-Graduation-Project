using MediatR;

namespace RealEstate.Shared.Kernel.Shared.IntegrationEvents;

/// <summary>
/// Fired by the Identity module when an individual user (Owner or Broker) registers.
/// Subscription module handles this to auto-assign the Free Individual plan.
/// </summary>
public record IndividualUserCreatedEvent(Guid UserId, string AccountType) : INotification;
