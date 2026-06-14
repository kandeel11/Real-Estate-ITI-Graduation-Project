namespace Subscription.Application.Interfaces;

public interface ISubscriptionRepository
{
    Task<Domain.Entities.Subscription?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Domain.Entities.Subscription?> GetActiveByUserAsync(Guid userId, CancellationToken ct = default);
    Task<Domain.Entities.Subscription?> GetActiveByCompanyAsync(Guid companyId, CancellationToken ct = default);
    Task<Guid> CreateAsync(Domain.Entities.Subscription subscription, CancellationToken ct = default);
    Task UpdateAsync(Domain.Entities.Subscription subscription, CancellationToken ct = default);
}
