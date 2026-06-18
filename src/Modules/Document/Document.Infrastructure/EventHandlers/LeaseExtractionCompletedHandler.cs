using AI.Application.Events;
using Document.Domain.Enums;
using Document.Infrastructure.Persistence;
using MediatR;

namespace Document.Infrastructure.EventHandlers;

public class LeaseExtractionCompletedHandler : INotificationHandler<LeaseExtractionCompletedEvent>
{
    private readonly DocumentDbContext _db;

    public LeaseExtractionCompletedHandler(DocumentDbContext db) => _db = db;

    public async Task Handle(LeaseExtractionCompletedEvent evt, CancellationToken ct)
    {
        var lease = await _db.Leases.FindAsync(new object[] { evt.LeaseId }, ct);
        if (lease == null) return;

        var data = evt.ExtractedData;

        // ── Map extracted fields ──────────────────────────────────────────────
        lease.InstallmentAmount = decimal.TryParse(data.InstallmentAmount.Value, out var inst) ? inst : null;
        lease.SecurityDeposit   = decimal.TryParse(data.SecurityDeposit.Value, out var dep) ? dep : null;
        lease.StartDate         = DateOnly.TryParse(data.StartDate.Value, out var start) ? start : null;
        lease.EndDate           = DateOnly.TryParse(data.EndDate.Value, out var end) ? end : null;
        lease.PaymentDayOfMonth = int.TryParse(data.PaymentDayOfMonth.Value, out var day) ? day : null;
        lease.DurationInMonths  = int.TryParse(data.DurationInMonths.Value, out var dur) ? dur : null;
        lease.TotalRentAmount   = decimal.TryParse(data.TotalRentAmount.Value, out var total) ? total : null;

        if (Enum.TryParse<PaymentFrequency>(data.PaymentFrequency.Value, ignoreCase: true, out var freq))
            lease.PaymentFrequency = freq;

        // ── Backend gap-filling ───────────────────────────────────────────────
        // Rule 1: StartDate + DurationInMonths → calculate EndDate
        if (lease.StartDate.HasValue && lease.DurationInMonths.HasValue && !lease.EndDate.HasValue)
            lease.EndDate = lease.StartDate.Value.AddMonths(lease.DurationInMonths.Value);

        // Rule 2: StartDate + EndDate → calculate DurationInMonths
        if (lease.StartDate.HasValue && lease.EndDate.HasValue && !lease.DurationInMonths.HasValue)
        {
            var months = ((lease.EndDate.Value.Year - lease.StartDate.Value.Year) * 12)
                       + lease.EndDate.Value.Month - lease.StartDate.Value.Month;
            lease.DurationInMonths = months > 0 ? months : null;
        }

        // Rule 3: TotalRentAmount null → calculate from InstallmentAmount × numberOfInstallments
        if (!lease.TotalRentAmount.HasValue && lease.InstallmentAmount.HasValue
            && lease.DurationInMonths.HasValue && lease.PaymentFrequency.HasValue)
        {
            var monthsPerInstallment = lease.PaymentFrequency.Value switch
            {
                PaymentFrequency.Monthly      => 1,
                PaymentFrequency.Quarterly    => 3,
                PaymentFrequency.SemiAnnually => 6,
                PaymentFrequency.Annually     => 12,
                _                             => 1
            };
            var numberOfInstallments = lease.DurationInMonths.Value / monthsPerInstallment;
            if (numberOfInstallments > 0)
                lease.TotalRentAmount = lease.InstallmentAmount.Value * numberOfInstallments;
        }

        lease.ExtractionStatus     = LeaseExtractionStatus.Extracted;
        lease.ExtractionConfidence = data.OverallConfidence;
        lease.UpdatedAt            = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }
}
