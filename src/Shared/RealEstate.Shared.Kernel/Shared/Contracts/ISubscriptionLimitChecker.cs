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
}
