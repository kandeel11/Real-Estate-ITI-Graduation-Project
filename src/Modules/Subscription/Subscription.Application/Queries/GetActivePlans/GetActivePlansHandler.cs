using MediatR;
using Subscription.Application.DTOs;
using Subscription.Application.Interfaces;

namespace Subscription.Application.Queries.GetActivePlans;

public class GetActivePlansHandler : IRequestHandler<GetActivePlansQuery, List<SubscriptionPlanDto>>
{
    private readonly ISubscriptionPlanRepository _repository;

    public GetActivePlansHandler(ISubscriptionPlanRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<SubscriptionPlanDto>> Handle(GetActivePlansQuery request, CancellationToken cancellationToken)
    {
        var plans = await _repository.GetActiveAsync(cancellationToken);
        return plans.Select(p => new SubscriptionPlanDto
        {
            Id = p.Id,
            Name = p.Name,
            Tier = p.Tier,
            PriceMonthly = p.PriceMonthly,
            PriceAnnual = p.PriceAnnual,
            MaxProperties = p.MaxProperties,
            MaxBrokers = p.MaxBrokers,
            MaxAiRequestsPerMonth = p.MaxAiRequestsPerMonth,
            MaxStorageGb = p.MaxStorageGb,
            HasAdvancedAnalytics = p.HasAdvancedAnalytics,
            HasFeaturedListings = p.HasFeaturedListings,
            HasAiPriceEstimation = p.HasAiPriceEstimation,
            HasAiChatbot = p.HasAiChatbot,
            HasAiLeadPrioritization = p.HasAiLeadPrioritization,
            HasAiDescriptionGenerator = p.HasAiDescriptionGenerator,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt
        }).ToList();
    }
}
