using MediatR;
using Subscription.Application.Interfaces;
using Subscription.Domain.Enums;

namespace Subscription.Application.Commands.CancelMySubscription;

/// <summary>
/// "Cancelling" a premium subscription means dropping back to the FREE plan
/// of the same audience — the subscription itself stays active so limits and
/// dashboards keep working. Only premium tiers can be cancelled.
/// </summary>
public class CancelMySubscriptionHandler : IRequestHandler<CancelMySubscriptionCommand>
{
    private static readonly DateTime FreeForever = new(2099, 12, 31);

    private readonly ISubscriptionRepository _subscriptions;
    private readonly ISubscriptionPlanRepository _plans;

    public CancelMySubscriptionHandler(
        ISubscriptionRepository subscriptions,
        ISubscriptionPlanRepository plans)
    {
        _subscriptions = subscriptions;
        _plans = plans;
    }

    public async Task Handle(CancelMySubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = request.CompanyId is not null
            ? await _subscriptions.GetActiveByCompanyAsync(request.CompanyId.Value, cancellationToken)
            : await _subscriptions.GetActiveByUserAsync(request.UserId, cancellationToken);

        if (subscription is null)
            throw new KeyNotFoundException("No active subscription found.");

        var currentPlan = await _plans.GetByIdAsync(subscription.PlanId, cancellationToken)
            ?? throw new KeyNotFoundException("Current plan not found.");

        var freeTier = currentPlan.Tier switch
        {
            SubscriptionTier.PremiumIndividual => SubscriptionTier.FreeIndividual,
            SubscriptionTier.PremiumCompany => SubscriptionTier.FreeCompany,
            SubscriptionTier.PremiumTenant => SubscriptionTier.FreeTenant,
            _ => throw new InvalidOperationException("You are already on the free plan."),
        };

        var freePlan = await _plans.GetByTierAsync(freeTier, cancellationToken)
            ?? throw new InvalidOperationException("The free plan is not available.");

        subscription.Upgrade(freePlan.Id);          // records the downgrade + raises the event
        subscription.Renew(FreeForever);            // free plans never expire (project convention)
        await _subscriptions.UpdateAsync(subscription, cancellationToken);
    }
}
