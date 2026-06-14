using Microsoft.EntityFrameworkCore;
using Subscription.Application.Interfaces;
using Subscription.Domain.Entities;
using Subscription.Infrastructure.Persistence;

namespace Subscription.Infrastructure.Repositories;

public class SubscriptionUsageRepository : ISubscriptionUsageRepository
{
    private readonly SubscriptionDbContext _context;

    public SubscriptionUsageRepository(SubscriptionDbContext context)
    {
        _context = context;
    }

    public async Task<SubscriptionUsage?> GetCurrentPeriodAsync(Guid subscriptionId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _context.Usage
            .Where(u => u.SubscriptionId == subscriptionId && u.PeriodStart <= now && u.PeriodEnd >= now)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Guid> CreateAsync(SubscriptionUsage usage, CancellationToken ct = default)
    {
        await _context.Usage.AddAsync(usage, ct);
        await _context.SaveChangesAsync(ct);
        return usage.Id;
    }

    public async Task UpdateAsync(SubscriptionUsage usage, CancellationToken ct = default)
    {
        _context.Usage.Update(usage);
        await _context.SaveChangesAsync(ct);
    }
}
