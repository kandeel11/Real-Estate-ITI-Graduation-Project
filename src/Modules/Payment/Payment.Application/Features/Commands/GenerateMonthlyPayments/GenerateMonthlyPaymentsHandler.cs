using MediatR;
using Microsoft.Extensions.Logging;
using Payment.Application.Helpers;
using Payment.Application.Interfaces;
using PaymentEntity = Payment.Domain.Entities.Payment;

namespace Payment.Application.Features.Commands.GenerateMonthlyPayments;

/// <summary>
/// Handler بتاع GenerateMonthlyPaymentsCommand
/// 
/// شغله: يجيب كل الـ leases الأكتيف من الـ ILeaseService،
/// ويعمل دفعة Pending لكل lease مالهاش دفعة في الشهر ده
/// </summary>
public sealed class GenerateMonthlyPaymentsHandler
    : IRequestHandler<GenerateMonthlyPaymentsCommand, GenerateMonthlyPaymentsResult>
{
    private readonly IPaymentRepository _repository;
    private readonly ILeaseService _leaseService;
    private readonly ILogger<GenerateMonthlyPaymentsHandler> _logger;

    public GenerateMonthlyPaymentsHandler(
        IPaymentRepository repository,
        ILeaseService leaseService,
        ILogger<GenerateMonthlyPaymentsHandler> logger)
    {
        _repository = repository;
        _leaseService = leaseService;
        _logger = logger;
    }

    public async Task<GenerateMonthlyPaymentsResult> Handle(
        GenerateMonthlyPaymentsCommand request,
        CancellationToken cancellationToken)
    {
        // حدد الشهر المستهدف: لو مفيش شهر محدد، استخدم الشهر الحالي
        var targetMonth = request.TargetMonth ?? DateOnly.FromDateTime(DateTime.UtcNow);

        _logger.LogInformation(
            "Starting monthly payment generation for {Month}/{Year}",
            targetMonth.Month, targetMonth.Year);

        // جيب كل الـ leases الأكتيف من الـ Lease Module
        var activeLeases = await _leaseService.GetActiveLeasesAsync(cancellationToken);

        if (!activeLeases.Any())
        {
            _logger.LogWarning("No active leases found for payment generation.");
            return new GenerateMonthlyPaymentsResult(0, 0, []);
        }

        var paymentsToCreate = new List<PaymentEntity>();
        var errors = new List<string>();
        int skippedCount = 0;

        foreach (var lease in activeLeases)
        {
            try
            {
                // Skip leases where this month is not a period-start month for their frequency.
                // A Quarterly lease starting January only generates in Jan, Apr, Jul, Oct.
                if (!LeasePaymentHelper.IsPeriodStartMonth(lease.StartDate, lease.PaymentFrequency, targetMonth))
                {
                    _logger.LogDebug(
                        "Skipping LeaseId={LeaseId} — {Month}/{Year} is not a period-start month (frequency={Freq})",
                        lease.LeaseId, targetMonth.Month, targetMonth.Year, lease.PaymentFrequency ?? "Monthly");
                    skippedCount++;
                    continue;
                }

                var exactDueDate = LeasePaymentHelper.DueDateForMonth(
                    targetMonth.Year, targetMonth.Month, lease.PaymentDayOfMonth);

                // تحقق إن مفيش دفعة موجودة لنفس الـ lease في نفس الشهر
                var alreadyExists = await _repository.ExistsForLeaseAndMonthAsync(
                    lease.LeaseId, exactDueDate, cancellationToken);

                if (alreadyExists)
                {
                    _logger.LogDebug(
                        "Payment already exists for LeaseId={LeaseId} DueDate={DueDate}. Skipping.",
                        lease.LeaseId, exactDueDate);
                    skippedCount++;
                    continue;
                }

                var payment = PaymentEntity.CreateRent(
                    leaseId: lease.LeaseId,
                    propertyId: lease.PropertyId,
                    tenantId: lease.TenantId,
                    landlordId: lease.LandlordId,
                    amount: lease.InstallmentAmount,
                    dueDate: exactDueDate,
                    notes: $"Monthly rent for {targetMonth:MMMM yyyy}"
                );

                paymentsToCreate.Add(payment);
            }
            catch (Exception ex)
            {
                var errorMsg = $"Failed to generate payment for LeaseId={lease.LeaseId}: {ex.Message}";
                _logger.LogError(ex, errorMsg);
                errors.Add(errorMsg);
            }
        }

        if (paymentsToCreate.Any())
        {
            await _repository.AddRangeAsync(paymentsToCreate, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Monthly payment generation completed. Generated={Generated}, Skipped={Skipped}, Errors={Errors}",
            paymentsToCreate.Count, skippedCount, errors.Count);

        return new GenerateMonthlyPaymentsResult(
            paymentsToCreate.Count,
            skippedCount,
            errors);
    }
}
