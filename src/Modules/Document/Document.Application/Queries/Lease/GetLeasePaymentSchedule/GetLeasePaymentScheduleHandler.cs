using Document.Application.Interfaces;
using Document.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Queries.Lease.GetLeasePaymentSchedule;

public class GetLeasePaymentScheduleHandler
    : IRequestHandler<GetLeasePaymentScheduleQuery, PaymentScheduleDto?>
{
    private readonly IDocumentDbContext _db;

    public GetLeasePaymentScheduleHandler(IDocumentDbContext db) => _db = db;

    public async Task<PaymentScheduleDto?> Handle(
        GetLeasePaymentScheduleQuery query, CancellationToken ct)
    {
        var lease = await _db.Leases
            .Where(l => l.Id == query.LeaseId && l.DeletedAt == null)
            .FirstOrDefaultAsync(ct);

        if (lease is null) return null;

        if (!lease.InstallmentAmount.HasValue || !lease.PaymentFrequency.HasValue
            || !lease.StartDate.HasValue || !lease.DurationInMonths.HasValue)
            return null;

        var startDate        = lease.StartDate.Value;
        var durationMonths   = lease.DurationInMonths.Value;
        var installment      = lease.InstallmentAmount.Value;
        var frequency        = lease.PaymentFrequency.Value;
        var paymentDay       = lease.PaymentDayOfMonth ?? 1;
        var endDate          = lease.EndDate ?? startDate.AddMonths(durationMonths).AddDays(-1);
        var today            = DateOnly.FromDateTime(DateTime.UtcNow);

        var monthsPerPeriod = frequency switch
        {
            PaymentFrequency.Monthly      => 1,
            PaymentFrequency.Bimonthly    => 2,
            PaymentFrequency.Quarterly    => 3,
            PaymentFrequency.SemiAnnually => 6,
            PaymentFrequency.Annually     => 12,
            _                             => 1
        };

        var totalInstallments = (int)Math.Ceiling((double)durationMonths / monthsPerPeriod);

        // ── Build raw period list ─────────────────────────────────────────────
        var periods = new List<PaymentPeriodDto>(totalInstallments);
        for (var i = 0; i < totalInstallments; i++)
        {
            var periodStart = startDate.AddMonths(i * monthsPerPeriod);
            var periodEnd   = i == totalInstallments - 1
                ? endDate
                : startDate.AddMonths((i + 1) * monthsPerPeriod).AddDays(-1);

            var maxDay  = DateTime.DaysInMonth(periodStart.Year, periodStart.Month);
            var dueDate = new DateOnly(periodStart.Year, periodStart.Month,
                                       Math.Min(paymentDay, maxDay));

            var isCurrent = today >= periodStart && today <= periodEnd;
            var isPast    = today > periodEnd;

            periods.Add(new PaymentPeriodDto(
                PeriodNumber: i + 1,
                PeriodStart:  periodStart,
                PeriodEnd:    periodEnd,
                DueDate:      dueDate,
                Amount:       installment,
                IsCurrent:    isCurrent,
                IsNext:       false,
                IsPast:       isPast
            ));
        }

        // ── Mark the "next" period ────────────────────────────────────────────
        // Next = first future period after the current one.
        // If no current period (lease hasn't started yet), next = first period.
        var currentIndex = periods.FindIndex(p => p.IsCurrent);
        var nextIndex    = currentIndex >= 0
            ? currentIndex + 1
            : periods.FindIndex(p => !p.IsPast);

        var finalPeriods = periods
            .Select((p, idx) => p with { IsNext = idx == nextIndex })
            .ToList();

        var currentPeriod = finalPeriods.FirstOrDefault(p => p.IsCurrent);
        var nextPeriod    = finalPeriods.FirstOrDefault(p => p.IsNext);

        // ── Summary ───────────────────────────────────────────────────────────
        var pastCount    = finalPeriods.Count(p => p.IsPast);
        var futureCount  = finalPeriods.Count(p => !p.IsPast && !p.IsCurrent);
        var totalAmount  = lease.TotalRentAmount ?? installment * totalInstallments;
        var dueSoFar     = installment * (pastCount + (currentPeriod is null ? 0 : 1));

        var summary = new PaymentScheduleSummaryDto(
            TotalInstallments:   totalInstallments,
            PastInstallments:    pastCount,
            CurrentInstallments: currentPeriod is null ? 0 : 1,
            FutureInstallments:  futureCount,
            TotalAmount:         totalAmount,
            AmountDueSoFar:      dueSoFar,
            AmountRemaining:     totalAmount - dueSoFar
        );

        return new PaymentScheduleDto(
            LeaseId:           lease.Id,
            InstallmentAmount: installment,
            PaymentFrequency:  frequency.ToString(),
            TotalRentAmount:   totalAmount,
            DurationInMonths:  durationMonths,
            StartDate:         startDate,
            EndDate:           endDate,
            PaymentDayOfMonth: paymentDay,
            CurrentPeriod:     currentPeriod,
            NextPeriod:        nextPeriod,
            AllPeriods:        finalPeriods,
            Summary:           summary
        );
    }
}
