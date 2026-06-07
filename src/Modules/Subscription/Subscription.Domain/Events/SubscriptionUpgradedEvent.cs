using MediatR;

namespace Subscription.Domain.Events;

public record SubscriptionUpgradedEvent(Guid SubscriptionId, Guid FromPlanId, Guid ToPlanId, DateTime UpgradedAt) : INotification;
