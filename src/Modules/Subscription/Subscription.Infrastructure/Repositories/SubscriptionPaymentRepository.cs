using Microsoft.EntityFrameworkCore;
using Subscription.Application.Interfaces;
using Subscription.Domain.Entities;
using Subscription.Infrastructure.Persistence;

namespace Subscription.Infrastructure.Repositories;

public class SubscriptionPaymentRepository : ISubscriptionPaymentRepository
{
    private readonly SubscriptionDbContext _context;

    public SubscriptionPaymentRepository(SubscriptionDbContext context)
    {
        _context = context;
    }

    public async Task<SubscriptionPayment?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Payments.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IEnumerable<SubscriptionPayment>> GetBySubscriptionAsync(Guid subscriptionId, CancellationToken ct = default)
        => await _context.Payments.Where(p => p.SubscriptionId == subscriptionId).ToListAsync(ct);

    public async Task<Guid> CreateAsync(SubscriptionPayment payment, CancellationToken ct = default)
    {
        await _context.Payments.AddAsync(payment, ct);
        await _context.SaveChangesAsync(ct);
        return payment.Id;
    }

    public async Task UpdateAsync(SubscriptionPayment payment, CancellationToken ct = default)
    {
        _context.Payments.Update(payment);
        await _context.SaveChangesAsync(ct);
    }
}
