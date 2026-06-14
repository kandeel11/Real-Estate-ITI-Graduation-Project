using MediatR;
using Subscription.Application.Interfaces;

namespace Subscription.Application.Commands.UpdatePlanFeatures;

public class UpdatePlanFeaturesHandler : IRequestHandler<UpdatePlanFeaturesCommand>
{
    private readonly ISubscriptionPlanRepository _repository;

    public UpdatePlanFeaturesHandler(ISubscriptionPlanRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(UpdatePlanFeaturesCommand request, CancellationToken cancellationToken)
    {
        var plan = await _repository.GetByIdAsync(request.PlanId, cancellationToken)
            ?? throw new KeyNotFoundException($"Subscription plan {request.PlanId} not found.");

        plan.UpdateFeatureFlags(
            request.HasAdvancedAnalytics,
            request.HasFeaturedListings,
            request.HasAiPriceEstimation,
            request.HasAiChatbot,
            request.HasAiLeadPrioritization,
            request.HasAiDescriptionGenerator);

        await _repository.UpdateAsync(plan, cancellationToken);
    }
}
