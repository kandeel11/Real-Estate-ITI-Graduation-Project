using MediatR;
using Subscription.Domain.Enums;

namespace Subscription.Application.Commands.CreatePlan;

public record CreatePlanCommand(
    string Name,
    SubscriptionTier Tier,
    decimal PriceMonthly,
    decimal PriceAnnual,
    int MaxProperties,
    int MaxBrokers,
    int MaxAiRequestsPerMonth,
    int MaxStorageGb,
    bool HasAdvancedAnalytics,
    bool HasFeaturedListings,
    bool HasAiPriceEstimation,
    bool HasAiChatbot,
    bool HasAiLeadPrioritization,
    bool HasAiDescriptionGenerator
) : IRequest<Guid>;
