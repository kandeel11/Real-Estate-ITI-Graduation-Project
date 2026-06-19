using MediatR;
using Microsoft.Extensions.Logging;
using Payment.Application.Interfaces;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;
using PaymentEntity = Payment.Domain.Entities.Payment;

namespace Payment.Application.Features.EventHandlers;

public sealed class LeaseActivatedPaymentHandler
    : INotificationHandler<LeaseActivatedIntegrationEvent>
{
    private readonly IPaymentRepository _repository;
    private readonly ILogger<LeaseActivatedPaymentHandler> _logger;

    public LeaseActivatedPaymentHandler(
        IPaymentRepository repository,
        ILogger<LeaseActivatedPaymentHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Handle(LeaseActivatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Lease {LeaseId} activated — generating {Duration}-month payment schedule ({Frequency})",
            notification.LeaseId, notification.DurationInMonths, notification.PaymentFrequency);

        int monthsPerInstallment = notification.PaymentFrequency?.ToLowerInvariant() switch
        {
            "quarterly"     => 3,
            "semiannually"  => 6,
            "annually"      => 12,
            _               => 1   // Monthly (default)
        };

        int totalInstallments = (int)Math.Ceiling(
            (double)notification.DurationInMonths / monthsPerInstallment);

        var paymentsToCreate = new List<PaymentEntity>();

        for (int i = 0; i < totalInstallments; i++)
        {
            var baseDate = notification.StartDate.AddMonths(i * monthsPerInstallment);
            int day = Math.Min(notification.PaymentDayOfMonth,
                DateTime.DaysInMonth(baseDate.Year, baseDate.Month));
            var dueDate = new DateOnly(baseDate.Year, baseDate.Month, day);

            // skip installments that fall after the lease end date
            if (dueDate > notification.EndDate) break;

            bool exists = await _repository.ExistsForLeaseAndMonthAsync(
                notification.LeaseId, dueDate, cancellationToken);

            if (exists)
            {
                _logger.LogDebug(
                    "Payment already exists for lease {LeaseId} due {DueDate} — skipping",
                    notification.LeaseId, dueDate);
                continue;
            }

            var payment = PaymentEntity.CreateRent(
                leaseId: notification.LeaseId,
                propertyId: notification.PropertyId,
                tenantId: notification.TenantId,
                landlordId: notification.OwnerId,
                amount: notification.InstallmentAmount,
                dueDate: dueDate,
                notes: $"Installment {i + 1} of {totalInstallments} — {baseDate:MMMM yyyy}"
            );

            paymentsToCreate.Add(payment);
        }

        if (paymentsToCreate.Count > 0)
        {
            await _repository.AddRangeAsync(paymentsToCreate, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Created {Count} payment records for lease {LeaseId}",
                paymentsToCreate.Count, notification.LeaseId);
        }
        else
        {
            _logger.LogInformation(
                "All payment records already exist for lease {LeaseId} — nothing created",
                notification.LeaseId);
        }
    }
}
