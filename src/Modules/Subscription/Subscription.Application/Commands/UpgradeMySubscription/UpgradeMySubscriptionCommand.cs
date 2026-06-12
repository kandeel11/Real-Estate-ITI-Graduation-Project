using MediatR;

namespace Subscription.Application.Commands.UpgradeMySubscription;

/// <summary>
/// Switches the caller's active subscription to another plan. Charging is
/// the Payment module's job (Phase 2) — this only moves the plan, raising
/// SubscriptionUpgradedEvent for Payment to pick up later.
/// </summary>
public record UpgradeMySubscriptionCommand(
    Guid UserId,
    Guid? CompanyId,
    Guid NewPlanId) : IRequest;
