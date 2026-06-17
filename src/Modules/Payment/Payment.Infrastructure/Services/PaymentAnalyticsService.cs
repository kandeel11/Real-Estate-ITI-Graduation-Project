using Microsoft.EntityFrameworkCore;
using Payment.Domain.Enums;
using Payment.Infrastructure.Persistence;
using RealEstate.Shared.Kernel.Analytics;

namespace Payment.Infrastructure.Services;

public sealed class PaymentAnalyticsService : IPaymentAnalyticsService
{
    private readonly PaymentDbContext _db;

    public PaymentAnalyticsService(PaymentDbContext db) => _db = db;

    public async Task<PaymentAnalyticsSummaryDto> GetSummaryAsync(CancellationToken ct = default)
    {
        var q = _db.Payments.AsNoTracking().Where(p => p.PaymentType == PaymentType.Rent);

        var totalCollected = await q.Where(p => p.Status == PaymentStatus.Paid).SumAsync(p => p.Amount, ct);
        var totalPending   = await q.Where(p => p.Status == PaymentStatus.Pending).SumAsync(p => p.Amount, ct);
        var totalLateFees  = await q.SumAsync(p => p.LateFee, ct);
        var overdueCount   = await q.CountAsync(p => p.Status == PaymentStatus.Late, ct);
        var paidCount      = await q.CountAsync(p => p.Status == PaymentStatus.Paid, ct);

        return new PaymentAnalyticsSummaryDto(totalCollected, totalPending, totalLateFees, overdueCount, paidCount);
    }

    public async Task<decimal> GetRevenueInRangeAsync(DateTime from, DateTime to, CancellationToken ct = default)
        => await _db.Payments
            .AsNoTracking()
            .Where(p => p.PaymentType == PaymentType.Rent
                     && p.Status == PaymentStatus.Paid
                     && p.PaidAt.HasValue
                     && p.PaidAt.Value >= from
                     && p.PaidAt.Value <= to)
            .SumAsync(p => p.Amount, ct);
}
