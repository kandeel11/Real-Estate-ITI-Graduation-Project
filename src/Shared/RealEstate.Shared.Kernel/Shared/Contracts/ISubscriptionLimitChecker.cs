namespace RealEstate.Shared.Kernel.Shared.Contracts;

/// <summary>
/// Used by feature modules (Property, Broker, AI) to check and record subscription usage
/// without directly depending on the Subscription module.
/// Implemented by Subscription.Infrastructure and registered in SubscriptionModuleExtensions.
/// </summary>
public interface ISubscriptionLimitChecker
{
    Task<(bool Allowed, string Reason)> CheckPropertyLimitAsync(Guid userId, Guid? companyId, CancellationToken ct = default);
    Task IncrementPropertyUsageAsync(Guid userId, Guid? companyId, CancellationToken ct = default);
    Task DecrementPropertyUsageAsync(Guid userId, Guid? companyId, CancellationToken ct = default);

    Task<(bool Allowed, string Reason)> CheckBrokerLimitAsync(Guid companyId, CancellationToken ct = default);
    Task IncrementBrokerUsageAsync(Guid companyId, CancellationToken ct = default);

    /// <summary>
    /// Realigns the broker usage counter to the caller-supplied count of
    /// ACTIVE brokers (the Identity module knows the truth; the counter
    /// otherwise drifts because deactivate/delete never decremented it).
    /// </summary>
    Task SyncBrokerUsageAsync(Guid companyId, int activeBrokerCount, CancellationToken ct = default);

    Task<(bool Allowed, string Reason)> CheckAiRequestLimitAsync(Guid userId, Guid? companyId, CancellationToken ct = default);
    Task IncrementAiUsageAsync(Guid userId, Guid? companyId, CancellationToken ct = default);

    Task<(bool Allowed, string Reason)> CheckInquiryLimitAsync(Guid userId, CancellationToken ct = default);
    Task IncrementInquiryUsageAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Saved listings (tenant wishlist) are a standing count, not a monthly quota,
    /// so the caller supplies how many the tenant currently has saved and we compare
    /// it against the plan's MaxSavedListings. No usage counter is maintained.
    /// </summary>
    Task<(bool Allowed, string Reason)> CheckSavedListingLimitAsync(Guid userId, int currentSavedCount, CancellationToken ct = default);

    Task<(bool Allowed, string Reason)> CheckStorageLimitAsync(Guid userId, Guid? companyId, decimal additionalGb, CancellationToken ct = default);
    Task AddStorageUsageAsync(Guid userId, Guid? companyId, decimal gb, CancellationToken ct = default);

    /// <summary>Plan FEATURE gates (separate from the request quota).</summary>
    Task<(bool Allowed, string Reason)> CheckAiChatbotAccessAsync(Guid userId, Guid? companyId, CancellationToken ct = default);
    Task<(bool Allowed, string Reason)> CheckAiDescriptionAccessAsync(Guid userId, Guid? companyId, CancellationToken ct = default);
    Task<(bool Allowed, string Reason)> CheckFeaturedListingsAccessAsync(Guid userId, Guid? companyId, CancellationToken ct = default);
}
