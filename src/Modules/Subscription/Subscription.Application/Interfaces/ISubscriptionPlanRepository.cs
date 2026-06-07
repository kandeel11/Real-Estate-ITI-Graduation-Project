using Subscription.Domain.Entities;
using Subscription.Domain.Enums;

namespace Subscription.Application.Interfaces;

public interface ISubscriptionPlanRepository
{
    Task<SubscriptionPlan?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<SubscriptionPlan?> GetByTierAsync(SubscriptionTier tier, CancellationToken ct = default);
    Task<IEnumerable<SubscriptionPlan>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<SubscriptionPlan>> GetActiveAsync(CancellationToken ct = default);
    Task<Guid> CreateAsync(SubscriptionPlan plan, CancellationToken ct = default);
    Task UpdateAsync(SubscriptionPlan plan, CancellationToken ct = default);
}
