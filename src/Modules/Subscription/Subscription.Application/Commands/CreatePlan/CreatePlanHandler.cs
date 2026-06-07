using MediatR;
using Subscription.Application.Interfaces;
using Subscription.Domain.Entities;

namespace Subscription.Application.Commands.CreatePlan;

public class CreatePlanHandler : IRequestHandler<CreatePlanCommand, Guid>
{
    private readonly ISubscriptionPlanRepository _repository;

    public CreatePlanHandler(ISubscriptionPlanRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(CreatePlanCommand request, CancellationToken cancellationToken)
    {
        var plan = SubscriptionPlan.Create(
            request.Name,
            request.Tier,
            request.PriceMonthly,
            request.PriceAnnual,
            request.MaxProperties,
            request.MaxBrokers,
            request.MaxAiRequestsPerMonth,
            request.MaxStorageGb,
            request.HasAdvancedAnalytics,
            request.HasFeaturedListings,
            request.HasAiPriceEstimation,
            request.HasAiChatbot,
            request.HasAiLeadPrioritization,
            request.HasAiDescriptionGenerator);

        return await _repository.CreateAsync(plan, cancellationToken);
    }
}
