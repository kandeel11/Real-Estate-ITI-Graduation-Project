using MediatR;
using Payment.Application.Interfaces;
using Payment.Domain.Enums;

namespace Payment.Application.Features.Queries.GetOwnerFinancialSummary;

public sealed class GetOwnerFinancialSummaryHandler
    : IRequestHandler<GetOwnerFinancialSummaryQuery, OwnerFinancialSummaryDto>
{
    private readonly IPaymentRepository _repository;

    public GetOwnerFinancialSummaryHandler(IPaymentRepository repository)
        => _repository = repository;

    public async Task<OwnerFinancialSummaryDto> Handle(
        GetOwnerFinancialSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var payments = await _repository.GetByLandlordIdAsync(request.OwnerId, cancellationToken);

        var paid    = payments.Where(p => p.Status is PaymentStatus.Paid or PaymentStatus.PaidExternally).ToList();
        var pending = payments.Where(p => p.Status is PaymentStatus.Pending or PaymentStatus.Late).ToList();

        var totalRevenue     = paid.Sum(p => p.Amount);
        var totalPlatformFee = paid.Where(p => p.Status == PaymentStatus.Paid).Sum(p => p.CommissionAmount);
        var totalLateFees    = paid.Sum(p => p.LateFee);
        var totalPending     = pending.Sum(p => p.Amount);

        // Last 12 months breakdown
        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-11);
        var monthly = paid
            .Where(p => p.DueDate >= cutoff)
            .GroupBy(p => new { p.DueDate.Year, p.DueDate.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new MonthlyRevenueDto(
                Month:   new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM"),
                Year:    g.Key.Year,
                Revenue: g.Sum(p => p.Amount),
                Count:   g.Count()
            ))
            .ToList();

        return new OwnerFinancialSummaryDto(
            TotalRevenue:     totalRevenue,
            TotalPending:     totalPending,
            TotalPlatformFee: totalPlatformFee,
            TotalLateFees:    totalLateFees,
            TotalPaidCount:   paid.Count,
            TotalPendingCount: pending.Count,
            Monthly:          monthly
        );
    }
}
