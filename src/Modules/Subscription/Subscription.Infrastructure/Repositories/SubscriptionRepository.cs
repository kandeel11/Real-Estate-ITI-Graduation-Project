using Microsoft.EntityFrameworkCore;
using Subscription.Application.Interfaces;
using Subscription.Domain.Enums;
using Subscription.Infrastructure.Persistence;

namespace Subscription.Infrastructure.Repositories;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly SubscriptionDbContext _context;

    public SubscriptionRepository(SubscriptionDbContext context)
    {
        _context = context;
    }

    public async Task<Domain.Entities.Subscription?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Subscriptions.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<Domain.Entities.Subscription?> GetActiveByUserAsync(Guid userId, CancellationToken ct = default)
        => await _context.Subscriptions
            .Where(s => s.UserId == userId && (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial))
            .OrderByDescending(s => s.StartDate).ThenByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<Domain.Entities.Subscription?> GetActiveByCompanyAsync(Guid companyId, CancellationToken ct = default)
        => await _context.Subscriptions
            .Where(s => s.CompanyId == companyId && (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial))
            .OrderByDescending(s => s.StartDate).ThenByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<Guid> CreateAsync(Domain.Entities.Subscription subscription, CancellationToken ct = default)
    {
        await _context.Subscriptions.AddAsync(subscription, ct);
        await _context.SaveChangesAsync(ct);
        return subscription.Id;
    }

    public async Task UpdateAsync(Domain.Entities.Subscription subscription, CancellationToken ct = default)
    {
        _context.Subscriptions.Update(subscription);
        await _context.SaveChangesAsync(ct);
    }
}
