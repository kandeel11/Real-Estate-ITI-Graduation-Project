using MediatR;
using Subscription.Application.DTOs;
using Subscription.Application.Interfaces;

namespace Subscription.Application.Queries.GetMySubscription;

public class GetMySubscriptionHandler : IRequestHandler<GetMySubscriptionQuery, MySubscriptionDto?>
{
    private readonly ISubscriptionRepository _subscriptions;
    private readonly ISubscriptionPlanRepository _plans;

    public GetMySubscriptionHandler(
        ISubscriptionRepository subscriptions,
        ISubscriptionPlanRepository plans)
    {
        _subscriptions = subscriptions;
        _plans = plans;
    }

    public async Task<MySubscriptionDto?> Handle(GetMySubscriptionQuery request, CancellationToken cancellationToken)
    {
        var subscription = request.CompanyId is not null
            ? await _subscriptions.GetActiveByCompanyAsync(request.CompanyId.Value, cancellationToken)
            : await _subscriptions.GetActiveByUserAsync(request.UserId, cancellationToken);

        if (subscription is null) return null;

        var plan = await _plans.GetByIdAsync(subscription.PlanId, cancellationToken);
        if (plan is null) return null;

        return new MySubscriptionDto
        {
            Id = subscription.Id,
            PlanId = plan.Id,
            PlanName = plan.Name,
            Tier = plan.Tier.ToString(),
            Status = subscription.Status.ToString(),
            BillingCycle = subscription.BillingCycle.ToString(),
            PriceMonthly = plan.PriceMonthly,
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            IsAutoRenew = subscription.IsAutoRenew,
            MaxProperties = plan.MaxProperties,
            MaxBrokers = plan.MaxBrokers,
            MaxAiRequestsPerMonth = plan.MaxAiRequestsPerMonth,
            MaxStorageGb = plan.MaxStorageGb,
            MaxSavedListings = plan.MaxSavedListings,
            MaxInquiriesPerMonth = plan.MaxInquiriesPerMonth,
            HasAdvancedAnalytics = plan.HasAdvancedAnalytics,
            HasFeaturedListings = plan.HasFeaturedListings,
            HasAiPriceEstimation = plan.HasAiPriceEstimation,
            HasAiChatbot = plan.HasAiChatbot,
            HasAiLeadPrioritization = plan.HasAiLeadPrioritization,
            HasAiDescriptionGenerator = plan.HasAiDescriptionGenerator,
        };
    }
}
