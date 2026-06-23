using AI.Application.Events;
using Document.Domain.Enums;
using Document.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Document.Infrastructure.EventHandlers;

public class LeaseExtractionCompletedHandler : INotificationHandler<LeaseExtractionCompletedEvent>
{
    private readonly DocumentDbContext _db;
    private readonly ILogger<LeaseExtractionCompletedHandler> _logger;

    public LeaseExtractionCompletedHandler(
        DocumentDbContext db,
        ILogger<LeaseExtractionCompletedHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Handle(LeaseExtractionCompletedEvent evt, CancellationToken ct)
    {
        _logger.LogInformation(
            "[LeaseExtraction] LeaseExtractionCompletedEvent received | LeaseId={LeaseId} | OverallConfidence={Confidence}%",
            evt.LeaseId, evt.ExtractedData.OverallConfidence);

        var lease = await _db.Leases.FindAsync(new object[] { evt.LeaseId }, ct);
        if (lease == null)
        {
            _logger.LogWarning("[LeaseExtraction] Lease {LeaseId} not found in DB — event ignored.", evt.LeaseId);
            return;
        }

        var data = evt.ExtractedData;

        // ── Log raw AI fields before any parsing ──────────────────────────────
        _logger.LogInformation(
            "[LeaseExtraction] Raw AI fields for LeaseId={LeaseId}:\n" +
            "  installment_amount   = {IA} (confidence={IAC}%)\n" +
            "  payment_frequency    = {PF} (confidence={PFC}%)\n" +
            "  total_rent_amount    = {TR} (confidence={TRC}%)\n" +
            "  duration_in_months   = {DM} (confidence={DMC}%)\n" +
            "  security_deposit     = {SD} (confidence={SDC}%)\n" +
            "  start_date           = {ST} (confidence={STC}%)\n" +
            "  end_date             = {ED} (confidence={EDC}%)\n" +
            "  payment_day_of_month = {PD} (confidence={PDC}%)\n" +
            "  tenant_name          = {TN} (confidence={TNC}%)\n" +
            "  owner_name           = {ON} (confidence={ONC}%)",
            evt.LeaseId,
            data.InstallmentAmount.Value,  data.InstallmentAmount.ConfidenceScore,
            data.PaymentFrequency.Value,   data.PaymentFrequency.ConfidenceScore,
            data.TotalRentAmount.Value,    data.TotalRentAmount.ConfidenceScore,
            data.DurationInMonths.Value,   data.DurationInMonths.ConfidenceScore,
            data.SecurityDeposit.Value,    data.SecurityDeposit.ConfidenceScore,
            data.StartDate.Value,          data.StartDate.ConfidenceScore,
            data.EndDate.Value,            data.EndDate.ConfidenceScore,
            data.PaymentDayOfMonth.Value,  data.PaymentDayOfMonth.ConfidenceScore,
            data.TenantName.Value,         data.TenantName.ConfidenceScore,
            data.OwnerName.Value,          data.OwnerName.ConfidenceScore);

        // ── Map extracted fields ──────────────────────────────────────────────
        lease.InstallmentAmount = decimal.TryParse(data.InstallmentAmount.Value, out var inst) ? inst : null;
        if (lease.InstallmentAmount == null)
            _logger.LogWarning("[LeaseExtraction] LeaseId={LeaseId} — installment_amount '{Raw}' could not be parsed as decimal.", evt.LeaseId, data.InstallmentAmount.Value);

        lease.SecurityDeposit = decimal.TryParse(data.SecurityDeposit.Value, out var dep) ? dep : null;
        if (lease.SecurityDeposit == null && data.SecurityDeposit.Value != null)
            _logger.LogWarning("[LeaseExtraction] LeaseId={LeaseId} — security_deposit '{Raw}' could not be parsed as decimal.", evt.LeaseId, data.SecurityDeposit.Value);

        lease.StartDate = DateOnly.TryParse(data.StartDate.Value, out var start) ? start : null;
        if (lease.StartDate == null)
            _logger.LogWarning("[LeaseExtraction] LeaseId={LeaseId} — start_date '{Raw}' could not be parsed as DateOnly.", evt.LeaseId, data.StartDate.Value);

        lease.EndDate = DateOnly.TryParse(data.EndDate.Value, out var end) ? end : null;
        if (lease.EndDate == null)
            _logger.LogWarning("[LeaseExtraction] LeaseId={LeaseId} — end_date '{Raw}' could not be parsed as DateOnly.", evt.LeaseId, data.EndDate.Value);

        lease.PaymentDayOfMonth = int.TryParse(data.PaymentDayOfMonth.Value, out var day) ? day : null;
        if (lease.PaymentDayOfMonth == null)
            _logger.LogWarning("[LeaseExtraction] LeaseId={LeaseId} — payment_day_of_month '{Raw}' could not be parsed as int.", evt.LeaseId, data.PaymentDayOfMonth.Value);

        lease.DurationInMonths = int.TryParse(data.DurationInMonths.Value, out var dur) ? dur : null;
        if (lease.DurationInMonths == null && data.DurationInMonths.Value != null)
            _logger.LogWarning("[LeaseExtraction] LeaseId={LeaseId} — duration_in_months '{Raw}' could not be parsed as int.", evt.LeaseId, data.DurationInMonths.Value);

        lease.TotalRentAmount = decimal.TryParse(data.TotalRentAmount.Value, out var total) ? total : null;
        if (lease.TotalRentAmount == null && data.TotalRentAmount.Value != null)
            _logger.LogWarning("[LeaseExtraction] LeaseId={LeaseId} — total_rent_amount '{Raw}' could not be parsed as decimal.", evt.LeaseId, data.TotalRentAmount.Value);

        if (Enum.TryParse<PaymentFrequency>(data.PaymentFrequency.Value, ignoreCase: true, out var freq))
            lease.PaymentFrequency = freq;
        else
            _logger.LogWarning("[LeaseExtraction] LeaseId={LeaseId} — payment_frequency '{Raw}' is not a valid PaymentFrequency enum value.", evt.LeaseId, data.PaymentFrequency.Value);

        // ── Backend gap-filling ───────────────────────────────────────────────
        if (lease.StartDate.HasValue && lease.DurationInMonths.HasValue && !lease.EndDate.HasValue)
        {
            lease.EndDate = lease.StartDate.Value.AddMonths(lease.DurationInMonths.Value);
            _logger.LogInformation("[LeaseExtraction] LeaseId={LeaseId} — Gap-fill Rule 1: EndDate calculated as {EndDate} (StartDate + {Months} months).", evt.LeaseId, lease.EndDate, lease.DurationInMonths);
        }

        if (lease.StartDate.HasValue && lease.EndDate.HasValue && !lease.DurationInMonths.HasValue)
        {
            var months = ((lease.EndDate.Value.Year - lease.StartDate.Value.Year) * 12)
                       + lease.EndDate.Value.Month - lease.StartDate.Value.Month;
            lease.DurationInMonths = months > 0 ? months : null;
            _logger.LogInformation("[LeaseExtraction] LeaseId={LeaseId} — Gap-fill Rule 2: DurationInMonths calculated as {Months}.", evt.LeaseId, lease.DurationInMonths);
        }

        if (!lease.TotalRentAmount.HasValue && lease.InstallmentAmount.HasValue
            && lease.DurationInMonths.HasValue && lease.PaymentFrequency.HasValue)
        {
            var monthsPerInstallment = lease.PaymentFrequency.Value switch
            {
                PaymentFrequency.Monthly      => 1,
                PaymentFrequency.Bimonthly    => 2,
                PaymentFrequency.Quarterly    => 3,
                PaymentFrequency.SemiAnnually => 6,
                PaymentFrequency.Annually     => 12,
                _                             => 1
            };
            var numberOfInstallments = lease.DurationInMonths.Value / monthsPerInstallment;
            if (numberOfInstallments > 0)
            {
                lease.TotalRentAmount = lease.InstallmentAmount.Value * numberOfInstallments;
                _logger.LogInformation(
                    "[LeaseExtraction] LeaseId={LeaseId} — Gap-fill Rule 3: TotalRentAmount={Total} ({Installment} × {Count} installments).",
                    evt.LeaseId, lease.TotalRentAmount, lease.InstallmentAmount, numberOfInstallments);
            }
        }

        lease.ExtractionStatus     = LeaseExtractionStatus.Extracted;
        lease.ExtractionConfidence = data.OverallConfidence;
        lease.UpdatedAt            = DateTime.UtcNow;

        _logger.LogInformation(
            "[LeaseExtraction] Final lease state for LeaseId={LeaseId}:\n" +
            "  ExtractionStatus    = {Status}\n" +
            "  InstallmentAmount   = {IA}\n" +
            "  PaymentFrequency    = {PF}\n" +
            "  TotalRentAmount     = {TR}\n" +
            "  DurationInMonths    = {DM}\n" +
            "  SecurityDeposit     = {SD}\n" +
            "  StartDate           = {ST}\n" +
            "  EndDate             = {ED}\n" +
            "  PaymentDayOfMonth   = {PD}\n" +
            "  ExtractionConfidence= {Conf}%",
            evt.LeaseId, lease.ExtractionStatus,
            lease.InstallmentAmount, lease.PaymentFrequency,
            lease.TotalRentAmount, lease.DurationInMonths,
            lease.SecurityDeposit, lease.StartDate, lease.EndDate,
            lease.PaymentDayOfMonth, lease.ExtractionConfidence);

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("[LeaseExtraction] LeaseId={LeaseId} saved to DB successfully.", evt.LeaseId);
    }
}
