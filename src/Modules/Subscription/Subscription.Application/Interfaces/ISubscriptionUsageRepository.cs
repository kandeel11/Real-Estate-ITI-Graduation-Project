using Subscription.Domain.Entities;

namespace Subscription.Application.Interfaces;

public interface ISubscriptionUsageRepository
{
    Task<SubscriptionUsage?> GetCurrentPeriodAsync(Guid subscriptionId, CancellationToken ct = default);
    Task<Guid> CreateAsync(SubscriptionUsage usage, CancellationToken ct = default);
    Task UpdateAsync(SubscriptionUsage usage, CancellationToken ct = default);
}
