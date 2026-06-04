using Domain.Entities;
using Subscription.Domain.Enums;

namespace Subscription.Domain.Entities;

public class SubscriptionPlan : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public SubscriptionTier Tier { get; private set; }
    public decimal PriceMonthly { get; private set; }
    public decimal PriceAnnual { get; private set; }

    public int MaxProperties { get; private set; }
    public int MaxBrokers { get; private set; }
    public int MaxAiRequestsPerMonth { get; private set; }
    public int MaxStorageGb { get; private set; }

    public bool HasAdvancedAnalytics { get; private set; }
    public bool HasFeaturedListings { get; private set; }
    public bool HasAiPriceEstimation { get; private set; }
    public bool HasAiChatbot { get; private set; }
    public bool HasAiLeadPrioritization { get; private set; }
    public bool HasAiDescriptionGenerator { get; private set; }

    public bool IsActive { get; private set; } = true;

    private readonly List<PlanFeature> _features = new();
    public ICollection<PlanFeature> Features => _features.AsReadOnly();

    private SubscriptionPlan() { }

    public static SubscriptionPlan Create(
        string name,
        SubscriptionTier tier,
        decimal priceMonthly,
        decimal priceAnnual,
        int maxProperties,
        int maxBrokers,
        int maxAiRequestsPerMonth,
        int maxStorageGb,
        bool hasAdvancedAnalytics,
        bool hasFeaturedListings,
        bool hasAiPriceEstimation,
        bool hasAiChatbot,
        bool hasAiLeadPrioritization,
        bool hasAiDescriptionGenerator)
    {
        return new SubscriptionPlan
        {
            Name = name,
            Tier = tier,
            PriceMonthly = priceMonthly,
            PriceAnnual = priceAnnual,
            MaxProperties = maxProperties,
            MaxBrokers = maxBrokers,
            MaxAiRequestsPerMonth = maxAiRequestsPerMonth,
            MaxStorageGb = maxStorageGb,
            HasAdvancedAnalytics = hasAdvancedAnalytics,
            HasFeaturedListings = hasFeaturedListings,
            HasAiPriceEstimation = hasAiPriceEstimation,
            HasAiChatbot = hasAiChatbot,
            HasAiLeadPrioritization = hasAiLeadPrioritization,
            HasAiDescriptionGenerator = hasAiDescriptionGenerator,
            IsActive = true
        };
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateFeatureFlags(
        bool hasAdvancedAnalytics,
        bool hasFeaturedListings,
        bool hasAiPriceEstimation,
        bool hasAiChatbot,
        bool hasAiLeadPrioritization,
        bool hasAiDescriptionGenerator)
    {
        HasAdvancedAnalytics = hasAdvancedAnalytics;
        HasFeaturedListings = hasFeaturedListings;
        HasAiPriceEstimation = hasAiPriceEstimation;
        HasAiChatbot = hasAiChatbot;
        HasAiLeadPrioritization = hasAiLeadPrioritization;
        HasAiDescriptionGenerator = hasAiDescriptionGenerator;
        UpdatedAt = DateTime.UtcNow;
    }
}
