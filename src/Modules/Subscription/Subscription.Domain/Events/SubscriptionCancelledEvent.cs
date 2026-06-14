using MediatR;

namespace Subscription.Domain.Events;

public record SubscriptionCancelledEvent(Guid SubscriptionId, string Reason, DateTime CancelledAt) : INotification;
