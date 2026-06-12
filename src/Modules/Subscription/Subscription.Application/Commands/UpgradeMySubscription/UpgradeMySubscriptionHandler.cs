using MediatR;
using Subscription.Application.Interfaces;
using Subscription.Domain.Enums;

namespace Subscription.Application.Commands.UpgradeMySubscription;

/// <summary>
/// Switches the caller to a new plan. Once the Payment module ships this will
/// run AFTER a successful payment; until then the change applies immediately
/// (SubscriptionUpgradedEvent is raised for Payment to hook into later).
/// Self-healing: if the caller somehow has no active subscription (e.g. data
/// from before cancel-meant-downgrade), choosing a plan creates one.
/// </summary>
public class UpgradeMySubscriptionHandler : IRequestHandler<UpgradeMySubscriptionCommand>
{
    private static readonly DateTime FreeForever = new(2099, 12, 31);

    private readonly ISubscriptionRepository _subscriptions;
    private readonly ISubscriptionPlanRepository _plans;

    public UpgradeMySubscriptionHandler(
        ISubscriptionRepository subscriptions,
        ISubscriptionPlanRepository plans)
    {
        _subscriptions = subscriptions;
        _plans = plans;
    }

    public async Task Handle(UpgradeMySubscriptionCommand request, CancellationToken cancellationToken)
    {
        var plan = await _plans.GetByIdAsync(request.NewPlanId, cancellationToken);
        if (plan is null || !plan.IsActive)
            throw new InvalidOperationException("The selected plan does not exist or is not available.");

        var endDate = plan.PriceMonthly == 0 ? FreeForever : DateTime.UtcNow.AddMonths(1);

        var subscription = request.CompanyId is not null
            ? await _subscriptions.GetActiveByCompanyAsync(request.CompanyId.Value, cancellationToken)
            : await _subscriptions.GetActiveByUserAsync(request.UserId, cancellationToken);

        if (subscription is null)
        {
            // No active subscription (legacy fully-cancelled data) — start fresh
            var fresh = Domain.Entities.Subscription.Create(
                planId: plan.Id,
                userId: request.UserId,
                companyId: request.CompanyId,
                billingCycle: BillingCycle.Monthly,
                startDate: DateTime.UtcNow,
                endDate: endDate,
                trialEndDate: null);

            await _subscriptions.CreateAsync(fresh, cancellationToken);
            return;
        }

        if (subscription.PlanId == request.NewPlanId)
            throw new InvalidOperationException("You are already on this plan.");

        subscription.Upgrade(request.NewPlanId);
        subscription.Renew(endDate);
        await _subscriptions.UpdateAsync(subscription, cancellationToken);
    }
}
