namespace RealEstate.Shared.Kernel.Analytics;

/// <summary>
/// Implemented by Subscription.Infrastructure. Exposes subscription revenue and
/// plan-distribution data to the AI module without touching SubscriptionDbContext.
/// </summary>
public interface ISubscriptionAnalyticsService
{
    Task<SubscriptionAnalyticsSummaryDto> GetSummaryAsync(CancellationToken ct = default);
    Task<int> GetNewSubscriptionsCountAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<int> GetChurnedSubscriptionsCountAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<PlanSubscriptionCountDto>> GetCountsByPlanAsync(CancellationToken ct = default);
}

public record SubscriptionAnalyticsSummaryDto(
    int TotalActive,
    int TotalTrial,
    int TotalExpiredOrCancelled,
    decimal TotalRevenueCollected);

public record PlanSubscriptionCountDto(string PlanName, string Tier, int ActiveCount);
