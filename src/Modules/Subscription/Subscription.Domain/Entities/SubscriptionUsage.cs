using Domain.Entities;

namespace Subscription.Domain.Entities;

public class SubscriptionUsage : BaseEntity
{
    public Guid SubscriptionId { get; private set; }
    public DateTime PeriodStart { get; private set; }
    public DateTime PeriodEnd { get; private set; }

    public int PropertiesUsed { get; private set; }
    public int BrokersUsed { get; private set; }
    public int AiRequestsUsed { get; private set; }
    public decimal StorageUsedGb { get; private set; }

    // Tenant counters
    public int SavedListingsUsed { get; private set; }
    public int SavedSearchesUsed { get; private set; }
    public int InquiriesUsed { get; private set; }

    public Subscription Subscription { get; private set; } = null!;

    private SubscriptionUsage() { }

    public static SubscriptionUsage Create(Guid subscriptionId, DateTime periodStart, DateTime periodEnd)
    {
        return new SubscriptionUsage
        {
            SubscriptionId = subscriptionId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            PropertiesUsed = 0,
            BrokersUsed = 0,
            AiRequestsUsed = 0,
            StorageUsedGb = 0m,
            SavedListingsUsed = 0,
            SavedSearchesUsed = 0,
            InquiriesUsed = 0
        };
    }

    public void IncrementAiUsage()
    {
        AiRequestsUsed++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementProperties()
    {
        PropertiesUsed++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementBrokers()
    {
        BrokersUsed++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddStorage(decimal gb)
    {
        StorageUsedGb += gb;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementSavedListings()
    {
        SavedListingsUsed++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DecrementSavedListings()
    {
        if (SavedListingsUsed > 0) SavedListingsUsed--;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementSavedSearches()
    {
        SavedSearchesUsed++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DecrementSavedSearches()
    {
        if (SavedSearchesUsed > 0) SavedSearchesUsed--;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementInquiries()
    {
        InquiriesUsed++;
        UpdatedAt = DateTime.UtcNow;
    }
}
