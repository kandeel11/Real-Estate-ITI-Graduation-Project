using MediatR;
using Microsoft.Extensions.Logging;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;

namespace Payment.Application.Features.Commands.GenerateMonthlyPayments;

public sealed class GenerateMonthlyPaymentsHandler
    : IRequestHandler<GenerateMonthlyPaymentsCommand, GenerateMonthlyPaymentsResult>
{
    private readonly IRentPaymentRepository _repository;
    private readonly ILeaseService _leaseService;
    private readonly ILogger<GenerateMonthlyPaymentsHandler> _logger;

    public GenerateMonthlyPaymentsHandler(
        IRentPaymentRepository repository,
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
        var targetMonth = request.TargetMonth ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var dueDate = new DateOnly(targetMonth.Year, targetMonth.Month, 1);

        _logger.LogInformation(
            "Starting monthly payment generation for {Month}/{Year}",
            targetMonth.Month, targetMonth.Year);

        var activeLeases = await _leaseService.GetActiveLeasesAsync(cancellationToken);

        if (!activeLeases.Any())
        {
            _logger.LogWarning("No active leases found for payment generation.");
            return new GenerateMonthlyPaymentsResult(0, 0, []);
        }

        var paymentsToCreate = new List<RentPayment>();
        var errors = new List<string>();
        int skippedCount = 0;

        foreach (var lease in activeLeases)
        {
            try
            {
                var paymentDay = Math.Min(lease.PaymentDayOfMonth,
                    DateTime.DaysInMonth(targetMonth.Year, targetMonth.Month));
                var exactDueDate = new DateOnly(targetMonth.Year, targetMonth.Month, paymentDay);

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

                var payment = RentPayment.Create(
                    leaseId: lease.LeaseId,
                    amount: lease.RentAmount,
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
