using Subscription.Domain.Enums;

namespace Subscription.Application.DTOs;

public record SubscriptionPlanDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public SubscriptionTier Tier { get; init; }
    public decimal PriceMonthly { get; init; }
    public decimal PriceAnnual { get; init; }
    public int MaxProperties { get; init; }
    public int MaxBrokers { get; init; }
    public int MaxAiRequestsPerMonth { get; init; }
    public int MaxStorageGb { get; init; }
    public bool HasAdvancedAnalytics { get; init; }
    public bool HasFeaturedListings { get; init; }
    public bool HasAiPriceEstimation { get; init; }
    public bool HasAiChatbot { get; init; }
    public bool HasAiLeadPrioritization { get; init; }
    public bool HasAiDescriptionGenerator { get; init; }
    public int MaxSavedListings { get; init; }
    public int MaxSavedSearches { get; init; }
    public int MaxInquiriesPerMonth { get; init; }
    public bool HasEarlyAccessListings { get; init; }
    public bool HasPriorityBrokerResponse { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}
