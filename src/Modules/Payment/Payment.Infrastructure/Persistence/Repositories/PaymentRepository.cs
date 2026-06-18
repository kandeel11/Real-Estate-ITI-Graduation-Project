using Microsoft.EntityFrameworkCore;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Payment.Domain.Enums;

namespace Payment.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementation بتاع IPaymentRepository
/// بيستخدم PaymentDbContext مباشرة — Payment هو single source of truth
/// لكل المعاملات المالية (Subscription, Rent, Transfer, Refund, Deposit, Withdrawal)
///
/// الـ Rent-specific queries (GetAllAsync, GetByLeaseIdAsync, GetOverdueAsync)
/// بتفلتر على PaymentType=Rent مع تضمين RentDetails
/// </summary>
public sealed class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _context;

    public PaymentRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<Payment.Domain.Entities.Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .Include(p => p.RentDetails)
            .Include(p => p.SubscriptionDetails)
            .Include(p => p.TransferDetails)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Payment.Domain.Entities.Payment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .Include(p => p.RentDetails)
            .Where(p => p.PaymentType == PaymentType.Rent)
            .OrderByDescending(p => p.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Payment.Domain.Entities.Payment>> GetByLeaseIdAsync(
        Guid leaseId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .Include(p => p.RentDetails)
            .Where(p => p.PaymentType == PaymentType.Rent && p.RentDetails!.LeaseId == leaseId)
            .OrderByDescending(p => p.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Payment.Domain.Entities.Payment>> GetOverdueAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return await _context.Payments
            .Include(p => p.RentDetails)
            .Where(p => p.PaymentType == PaymentType.Rent && p.Status == PaymentStatus.Pending && p.DueDate < today)
            .OrderBy(p => p.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsForLeaseAndMonthAsync(
        Guid leaseId,
        DateOnly dueDate,
        CancellationToken cancellationToken = default)
    {
        // نتحقق بالسنة والشهر — مش اليوم بالظبط
        return await _context.Payments
            .Where(p => p.PaymentType == PaymentType.Rent)
            .AnyAsync(p =>
                p.RentDetails!.LeaseId == leaseId &&
                p.DueDate.Year == dueDate.Year &&
                p.DueDate.Month == dueDate.Month,
                cancellationToken);
    }

    public async Task AddAsync(Payment.Domain.Entities.Payment payment, CancellationToken cancellationToken = default)
    {
        await _context.Payments.AddAsync(payment, cancellationToken);
    }

    public async Task AddRangeAsync(
        IEnumerable<Payment.Domain.Entities.Payment> payments,
        CancellationToken cancellationToken = default)
    {
        await _context.Payments.AddRangeAsync(payments, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
