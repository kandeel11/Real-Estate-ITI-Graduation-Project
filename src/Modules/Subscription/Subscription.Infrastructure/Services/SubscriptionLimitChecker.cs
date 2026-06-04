using RealEstate.Shared.Kernel.Shared.Contracts;
using Subscription.Application.Interfaces;
using Subscription.Domain.Entities;

namespace Subscription.Infrastructure.Services;

public class SubscriptionLimitChecker : ISubscriptionLimitChecker
{
    private readonly ISubscriptionRepository _subscriptionRepo;
    private readonly ISubscriptionPlanRepository _planRepo;
    private readonly ISubscriptionUsageRepository _usageRepo;

    public SubscriptionLimitChecker(
        ISubscriptionRepository subscriptionRepo,
        ISubscriptionPlanRepository planRepo,
        ISubscriptionUsageRepository usageRepo)
    {
        _subscriptionRepo = subscriptionRepo;
        _planRepo = planRepo;
        _usageRepo = usageRepo;
    }

    public async Task<(bool Allowed, string Reason)> CheckPropertyLimitAsync(Guid userId, Guid? companyId, CancellationToken ct = default)
    {
        var (subscription, plan, usage) = await ResolveAsync(userId, companyId, ct);
        if (subscription is null || plan is null)
            return (false, "No active subscription found.");

        if (usage.PropertiesUsed >= plan.MaxProperties)
            return (false, $"Property limit of {plan.MaxProperties} reached. Upgrade your plan to add more.");

        return (true, string.Empty);
    }

    public async Task IncrementPropertyUsageAsync(Guid userId, Guid? companyId, CancellationToken ct = default)
    {
        var (_, _, usage) = await ResolveAsync(userId, companyId, ct);
        usage.IncrementProperties();
        await _usageRepo.UpdateAsync(usage, ct);
    }

    public async Task<(bool Allowed, string Reason)> CheckBrokerLimitAsync(Guid companyId, CancellationToken ct = default)
    {
        var (subscription, plan, usage) = await ResolveAsync(Guid.Empty, companyId, ct);
        if (subscription is null || plan is null)
            return (false, "No active subscription found for this company.");

        if (usage.BrokersUsed >= plan.MaxBrokers)
            return (false, $"Broker limit of {plan.MaxBrokers} reached. Upgrade your plan to add more brokers.");

        return (true, string.Empty);
    }

    public async Task IncrementBrokerUsageAsync(Guid companyId, CancellationToken ct = default)
    {
        var (_, _, usage) = await ResolveAsync(Guid.Empty, companyId, ct);
        usage.IncrementBrokers();
        await _usageRepo.UpdateAsync(usage, ct);
    }

    public async Task<(bool Allowed, string Reason)> CheckAiRequestLimitAsync(Guid userId, Guid? companyId, CancellationToken ct = default)
    {
        var (subscription, plan, usage) = await ResolveAsync(userId, companyId, ct);
        if (subscription is null || plan is null)
            return (false, "No active subscription found.");

        if (usage.AiRequestsUsed >= plan.MaxAiRequestsPerMonth)
            return (false, $"AI request limit of {plan.MaxAiRequestsPerMonth}/month reached. Upgrade your plan for more AI features.");

        return (true, string.Empty);
    }

    public async Task IncrementAiUsageAsync(Guid userId, Guid? companyId, CancellationToken ct = default)
    {
        var (_, _, usage) = await ResolveAsync(userId, companyId, ct);
        usage.IncrementAiUsage();
        await _usageRepo.UpdateAsync(usage, ct);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private async Task<(Domain.Entities.Subscription? Subscription, SubscriptionPlan? Plan, SubscriptionUsage Usage)>
        ResolveAsync(Guid userId, Guid? companyId, CancellationToken ct)
    {
        var subscription = companyId.HasValue
            ? await _subscriptionRepo.GetActiveByCompanyAsync(companyId.Value, ct)
            : await _subscriptionRepo.GetActiveByUserAsync(userId, ct);

        if (subscription is null)
            return (null, null, null!);

        var plan = await _planRepo.GetByIdAsync(subscription.PlanId, ct);
        var usage = await GetOrCreateUsageAsync(subscription.Id, ct);

        return (subscription, plan, usage);
    }

    private async Task<SubscriptionUsage> GetOrCreateUsageAsync(Guid subscriptionId, CancellationToken ct)
    {
        var usage = await _usageRepo.GetCurrentPeriodAsync(subscriptionId, ct);
        if (usage is not null)
            return usage;

        // No record for this period — auto-create (monthly reset)
        var now = DateTime.UtcNow;
        var periodStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = periodStart.AddMonths(1).AddTicks(-1);
        var newUsage = SubscriptionUsage.Create(subscriptionId, periodStart, periodEnd);
        await _usageRepo.CreateAsync(newUsage, ct);
        return newUsage;
    }
}
