using MediatR;
using Subscription.Application.DTOs;
using Subscription.Application.Interfaces;

namespace Subscription.Application.Queries.GetPlanById;

public class GetPlanByIdHandler : IRequestHandler<GetPlanByIdQuery, SubscriptionPlanDto?>
{
    private readonly ISubscriptionPlanRepository _repository;

    public GetPlanByIdHandler(ISubscriptionPlanRepository repository)
    {
        _repository = repository;
    }

    public async Task<SubscriptionPlanDto?> Handle(GetPlanByIdQuery request, CancellationToken cancellationToken)
    {
        var plan = await _repository.GetByIdAsync(request.PlanId, cancellationToken);
        if (plan is null) return null;

        return new SubscriptionPlanDto
        {
            Id = plan.Id,
            Name = plan.Name,
            Tier = plan.Tier,
            PriceMonthly = plan.PriceMonthly,
            PriceAnnual = plan.PriceAnnual,
            MaxProperties = plan.MaxProperties,
            MaxBrokers = plan.MaxBrokers,
            MaxAiRequestsPerMonth = plan.MaxAiRequestsPerMonth,
            MaxStorageGb = plan.MaxStorageGb,
            HasAdvancedAnalytics = plan.HasAdvancedAnalytics,
            HasFeaturedListings = plan.HasFeaturedListings,
            HasAiPriceEstimation = plan.HasAiPriceEstimation,
            HasAiChatbot = plan.HasAiChatbot,
            HasAiLeadPrioritization = plan.HasAiLeadPrioritization,
            HasAiDescriptionGenerator = plan.HasAiDescriptionGenerator,
            IsActive = plan.IsActive,
            CreatedAt = plan.CreatedAt
        };
    }
}
