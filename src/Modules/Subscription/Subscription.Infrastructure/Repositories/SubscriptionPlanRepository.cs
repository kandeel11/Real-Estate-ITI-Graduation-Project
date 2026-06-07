using Microsoft.EntityFrameworkCore;
using Subscription.Application.Interfaces;
using Subscription.Domain.Entities;
using Subscription.Domain.Enums;
using Subscription.Infrastructure.Persistence;

namespace Subscription.Infrastructure.Repositories;

public class SubscriptionPlanRepository : ISubscriptionPlanRepository
{
    private readonly SubscriptionDbContext _context;

    public SubscriptionPlanRepository(SubscriptionDbContext context)
    {
        _context = context;
    }

    public async Task<SubscriptionPlan?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Plans.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<SubscriptionPlan?> GetByTierAsync(SubscriptionTier tier, CancellationToken ct = default)
        => await _context.Plans.FirstOrDefaultAsync(p => p.Tier == tier && p.IsActive, ct);

    public async Task<IEnumerable<SubscriptionPlan>> GetAllAsync(CancellationToken ct = default)
        => await _context.Plans.ToListAsync(ct);

    public async Task<IEnumerable<SubscriptionPlan>> GetActiveAsync(CancellationToken ct = default)
        => await _context.Plans.Where(p => p.IsActive).ToListAsync(ct);

    public async Task<Guid> CreateAsync(SubscriptionPlan plan, CancellationToken ct = default)
    {
        await _context.Plans.AddAsync(plan, ct);
        await _context.SaveChangesAsync(ct);
        return plan.Id;
    }

    public async Task UpdateAsync(SubscriptionPlan plan, CancellationToken ct = default)
    {
        _context.Plans.Update(plan);
        await _context.SaveChangesAsync(ct);
    }
}
