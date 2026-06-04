using MediatR;

namespace Subscription.Domain.Events;

public record SubscriptionPaymentProcessedEvent(Guid SubscriptionPaymentId, Guid SubscriptionId, decimal Amount, DateTime PaidAt) : INotification;
