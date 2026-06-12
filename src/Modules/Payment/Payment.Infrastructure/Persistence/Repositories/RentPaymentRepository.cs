using Microsoft.EntityFrameworkCore;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Payment.Domain.Enums;

namespace Payment.Infrastructure.Persistence.Repositories;

public sealed class RentPaymentRepository : IRentPaymentRepository
{
    private readonly PaymentDbContext _context;

    public RentPaymentRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<RentPayment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.RentPayments
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<RentPayment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.RentPayments
            .OrderByDescending(p => p.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RentPayment>> GetByLeaseIdAsync(
        Guid leaseId,
        CancellationToken cancellationToken = default)
    {
        return await _context.RentPayments
            .Where(p => p.LeaseId == leaseId)
            .OrderByDescending(p => p.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RentPayment>> GetOverdueAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return await _context.RentPayments
            .Where(p => p.Status == PaymentStatus.Pending && p.DueDate < today)
            .OrderBy(p => p.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsForLeaseAndMonthAsync(
        Guid leaseId,
        DateOnly dueDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.RentPayments
            .AnyAsync(p =>
                p.LeaseId == leaseId &&
                p.DueDate.Year == dueDate.Year &&
                p.DueDate.Month == dueDate.Month,
                cancellationToken);
    }

    public async Task AddAsync(RentPayment payment, CancellationToken cancellationToken = default)
    {
        await _context.RentPayments.AddAsync(payment, cancellationToken);
    }

    public async Task AddRangeAsync(
        IEnumerable<RentPayment> payments,
        CancellationToken cancellationToken = default)
    {
        await _context.RentPayments.AddRangeAsync(payments, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
