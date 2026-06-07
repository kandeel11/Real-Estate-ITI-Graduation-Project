using MediatR;

namespace Subscription.Application.Commands.UpdatePlanFeatures;

public record UpdatePlanFeaturesCommand(
    Guid PlanId,
    bool HasAdvancedAnalytics,
    bool HasFeaturedListings,
    bool HasAiPriceEstimation,
    bool HasAiChatbot,
    bool HasAiLeadPrioritization,
    bool HasAiDescriptionGenerator
) : IRequest;
