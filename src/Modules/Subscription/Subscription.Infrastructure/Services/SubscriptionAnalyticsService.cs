using Microsoft.EntityFrameworkCore;
using RealEstate.Shared.Kernel.Analytics;
using Subscription.Domain.Enums;
using Subscription.Infrastructure.Persistence;

namespace Subscription.Infrastructure.Services;

public sealed class SubscriptionAnalyticsService : ISubscriptionAnalyticsService
{
    private readonly SubscriptionDbContext _db;

    public SubscriptionAnalyticsService(SubscriptionDbContext db) => _db = db;

    public async Task<SubscriptionAnalyticsSummaryDto> GetSummaryAsync(CancellationToken ct = default)
    {
        var subs = _db.Subscriptions.AsNoTracking();

        var totalActive             = await subs.CountAsync(s => s.Status == SubscriptionStatus.Active, ct);
        var totalTrial              = await subs.CountAsync(s => s.Status == SubscriptionStatus.Trial, ct);
        var totalExpiredOrCancelled = await subs.CountAsync(
            s => s.Status == SubscriptionStatus.Expired || s.Status == SubscriptionStatus.Cancelled, ct);
        var totalRevenue            = await _db.Subscriptions
            .AsNoTracking()
            .Where(s => s.Status == SubscriptionStatus.Active)
            .Select(s => s.BillingCycle == BillingCycle.Annual
                ? s.Plan.PriceAnnual
                : s.BillingCycle == BillingCycle.Quarterly
                    ? s.Plan.PriceMonthly * 3
                    : s.Plan.PriceMonthly)
            .SumAsync(ct);

        return new SubscriptionAnalyticsSummaryDto(totalActive, totalTrial, totalExpiredOrCancelled, totalRevenue);
    }

    public async Task<int> GetNewSubscriptionsCountAsync(DateTime from, DateTime to, CancellationToken ct = default)
        => await _db.Subscriptions
            .AsNoTracking()
            .CountAsync(s => s.StartDate >= from && s.StartDate <= to, ct);

    public async Task<int> GetChurnedSubscriptionsCountAsync(DateTime from, DateTime to, CancellationToken ct = default)
        => await _db.Subscriptions
            .AsNoTracking()
            .CountAsync(s => s.CancelledAt != null && s.CancelledAt >= from && s.CancelledAt <= to, ct);

    public async Task<IReadOnlyList<PlanSubscriptionCountDto>> GetCountsByPlanAsync(CancellationToken ct = default)
    {
        var rows = await _db.Plans
            .AsNoTracking()
            .Where(p => p.IsActive)
            .Select(plan => new
            {
                plan.Name,
                plan.Tier,
                ActiveCount = _db.Subscriptions.Count(
                    s => s.PlanId == plan.Id && s.Status == SubscriptionStatus.Active)
            })
            .ToListAsync(ct);

        return rows
            .Select(x => new PlanSubscriptionCountDto(x.Name, x.Tier.ToString(), x.ActiveCount))
            .OrderByDescending(x => x.ActiveCount)
            .ToList();
    }
}
