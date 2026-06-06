using MediatR;

namespace Subscription.Domain.Events;

public record SubscriptionCreatedEvent(Guid SubscriptionId, Guid UserId, Guid PlanId, DateTime CreatedAt) : INotification;
