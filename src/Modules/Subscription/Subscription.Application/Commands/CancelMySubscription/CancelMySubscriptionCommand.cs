using MediatR;

namespace Subscription.Application.Commands.CancelMySubscription;

/// <summary>
/// Cancels the caller's active subscription, raising
/// SubscriptionCancelledEvent (downgrade handling is future work).
/// </summary>
public record CancelMySubscriptionCommand(
    Guid UserId,
    Guid? CompanyId,
    string Reason) : IRequest;
