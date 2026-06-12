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

        // No subscription yet (Identity not wired) — allow until auto-assignment is live
        if (subscription is null || plan is null)
            return (true, string.Empty);

        if (usage.PropertiesUsed >= plan.MaxProperties)
            return (false, $"Property limit of {plan.MaxProperties} reached. Upgrade your plan to add more.");

        return (true, string.Empty);
    }

    public async Task IncrementPropertyUsageAsync(Guid userId, Guid? companyId, CancellationToken ct = default)
    {
        var (_, _, usage) = await ResolveAsync(userId, companyId, ct);
        if (usage is null) return;
        usage.IncrementProperties();
        await _usageRepo.UpdateAsync(usage, ct);
    }

    public async Task DecrementPropertyUsageAsync(Guid userId, Guid? companyId, CancellationToken ct = default)
    {
        var (_, _, usage) = await ResolveAsync(userId, companyId, ct);
        if (usage is null) return;
        usage.DecrementProperties();
        await _usageRepo.UpdateAsync(usage, ct);
    }

    public async Task<(bool Allowed, string Reason)> CheckBrokerLimitAsync(Guid companyId, CancellationToken ct = default)
    {
        var (subscription, plan, usage) = await ResolveAsync(Guid.Empty, companyId, ct);
        if (subscription is null || plan is null)
            return (true, string.Empty);

        if (usage.BrokersUsed >= plan.MaxBrokers)
            return (false, $"Broker limit of {plan.MaxBrokers} reached. Upgrade your plan to add more brokers.");

        return (true, string.Empty);
    }

    public async Task IncrementBrokerUsageAsync(Guid companyId, CancellationToken ct = default)
    {
        var (_, _, usage) = await ResolveAsync(Guid.Empty, companyId, ct);
        if (usage is null) return;
        usage.IncrementBrokers();
        await _usageRepo.UpdateAsync(usage, ct);
    }

    public async Task SyncBrokerUsageAsync(Guid companyId, int activeBrokerCount, CancellationToken ct = default)
    {
        var (_, _, usage) = await ResolveAsync(Guid.Empty, companyId, ct);
        if (usage is null) return;
        usage.SetBrokers(activeBrokerCount);
        await _usageRepo.UpdateAsync(usage, ct);
    }

    public async Task<(bool Allowed, string Reason)> CheckAiRequestLimitAsync(Guid userId, Guid? companyId, CancellationToken ct = default)
    {
        var (subscription, plan, usage) = await ResolveAsync(userId, companyId, ct);
        if (subscription is null || plan is null)
            return (true, string.Empty);

        if (usage.AiRequestsUsed >= plan.MaxAiRequestsPerMonth)
            return (false, $"AI request limit of {plan.MaxAiRequestsPerMonth}/month reached. Upgrade your plan for more AI features.");

        return (true, string.Empty);
    }

    public async Task IncrementAiUsageAsync(Guid userId, Guid? companyId, CancellationToken ct = default)
    {
        var (_, _, usage) = await ResolveAsync(userId, companyId, ct);
        if (usage is null) return;
        usage.IncrementAiUsage();
        await _usageRepo.UpdateAsync(usage, ct);
    }

    public async Task<(bool Allowed, string Reason)> CheckInquiryLimitAsync(Guid userId, CancellationToken ct = default)
    {
        var (subscription, plan, usage) = await ResolveAsync(userId, null, ct);
        if (subscription is null || plan is null)
            return (true, string.Empty);

        if (usage.InquiriesUsed >= plan.MaxInquiriesPerMonth)
            return (false, $"Inquiry limit of {plan.MaxInquiriesPerMonth}/month reached. Upgrade your plan to send more inquiries.");

        return (true, string.Empty);
    }

    public async Task IncrementInquiryUsageAsync(Guid userId, CancellationToken ct = default)
    {
        var (_, _, usage) = await ResolveAsync(userId, null, ct);
        if (usage is null) return;
        usage.IncrementInquiries();
        await _usageRepo.UpdateAsync(usage, ct);
    }

    public async Task<(bool Allowed, string Reason)> CheckStorageLimitAsync(Guid userId, Guid? companyId, decimal additionalGb, CancellationToken ct = default)
    {
        var (subscription, plan, usage) = await ResolveAsync(userId, companyId, ct);
        if (subscription is null || plan is null)
            return (true, string.Empty);

        if (usage.StorageUsedGb + additionalGb > plan.MaxStorageGb)
            return (false, $"Storage limit of {plan.MaxStorageGb} GB reached. Upgrade your plan for more space.");

        return (true, string.Empty);
    }

    public async Task AddStorageUsageAsync(Guid userId, Guid? companyId, decimal gb, CancellationToken ct = default)
    {
        var (_, _, usage) = await ResolveAsync(userId, companyId, ct);
        if (usage is null) return;
        usage.AddStorage(gb);
        await _usageRepo.UpdateAsync(usage, ct);
    }

    public async Task<(bool Allowed, string Reason)> CheckAiChatbotAccessAsync(Guid userId, Guid? companyId, CancellationToken ct = default)
    {
        var (subscription, plan, _) = await ResolveAsync(userId, companyId, ct);
        if (subscription is null || plan is null)
            return (true, string.Empty);

        return plan.HasAiChatbot
            ? (true, string.Empty)
            : (false, "The AI chatbot is not included in your plan. Upgrade to use it.");
    }

    public async Task<(bool Allowed, string Reason)> CheckAiDescriptionAccessAsync(Guid userId, Guid? companyId, CancellationToken ct = default)
    {
        var (subscription, plan, _) = await ResolveAsync(userId, companyId, ct);
        if (subscription is null || plan is null)
            return (true, string.Empty);

        return plan.HasAiDescriptionGenerator
            ? (true, string.Empty)
            : (false, "The AI description generator is a premium feature. Upgrade your plan or write the description manually.");
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
