using Subscription.Domain.Entities;

namespace Subscription.Application.Interfaces;

public interface ISubscriptionPaymentRepository
{
    Task<SubscriptionPayment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<SubscriptionPayment>> GetBySubscriptionAsync(Guid subscriptionId, CancellationToken ct = default);
    Task<Guid> CreateAsync(SubscriptionPayment payment, CancellationToken ct = default);
    Task UpdateAsync(SubscriptionPayment payment, CancellationToken ct = default);
}
