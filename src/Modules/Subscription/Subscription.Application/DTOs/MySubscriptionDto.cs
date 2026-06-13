namespace Subscription.Application.DTOs;

/// <summary>
/// The caller's active subscription with its plan limits flattened in —
/// shaped for dashboard consumption (one call, no follow-up plan lookup).
/// Tier/Status are strings to match the Property module's enum convention.
/// </summary>
public record MySubscriptionDto
{
    public Guid Id { get; init; }
    public Guid PlanId { get; init; }
    public string PlanName { get; init; } = string.Empty;
    public string Tier { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string BillingCycle { get; init; } = string.Empty;
    public decimal PriceMonthly { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public bool IsAutoRenew { get; init; }

    public int MaxProperties { get; init; }
    public int MaxBrokers { get; init; }
    public int MaxAiRequestsPerMonth { get; init; }
    public int MaxStorageGb { get; init; }
    public int MaxSavedListings { get; init; }
    public int MaxInquiriesPerMonth { get; init; }

    public bool HasAdvancedAnalytics { get; init; }
    public bool HasFeaturedListings { get; init; }
    public bool HasAiPriceEstimation { get; init; }
    public bool HasAiChatbot { get; init; }
    public bool HasAiLeadPrioritization { get; init; }
    public bool HasAiDescriptionGenerator { get; init; }
}
