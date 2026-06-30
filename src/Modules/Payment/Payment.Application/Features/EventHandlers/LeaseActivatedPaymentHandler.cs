using MediatR;
using Microsoft.Extensions.Logging;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
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
            "Lease {LeaseId} activated — generating first installment ({Frequency})",
            notification.LeaseId, notification.PaymentFrequency);

        int day = Math.Min(notification.PaymentDayOfMonth,
            DateTime.DaysInMonth(notification.StartDate.Year, notification.StartDate.Month));
        var firstDueDate = new DateOnly(notification.StartDate.Year, notification.StartDate.Month, day);
        // Roll forward if pay-day falls before the lease start date
        // (e.g. start = Apr 29, pay-day = 1 → Apr 1 is before Apr 29 → move to May 1).
        if (firstDueDate < notification.StartDate)
        {
            var rolled = notification.StartDate.AddMonths(1);
            int rolledDay = Math.Min(notification.PaymentDayOfMonth, DateTime.DaysInMonth(rolled.Year, rolled.Month));
            firstDueDate = new DateOnly(rolled.Year, rolled.Month, rolledDay);
        }

        bool exists = await _repository.ExistsForLeaseAndMonthAsync(
            notification.LeaseId, firstDueDate, cancellationToken);

        if (exists)
        {
            _logger.LogInformation(
                "First payment already exists for lease {LeaseId} due {DueDate} — skipping",
                notification.LeaseId, firstDueDate);
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        bool isPast = firstDueDate < today;

        var payment = PaymentEntity.CreateRent(
            leaseId: notification.LeaseId,
            propertyId: notification.PropertyId,
            tenantId: notification.TenantId,
            landlordId: notification.OwnerId,
            amount: notification.InstallmentAmount,
            dueDate: firstDueDate,
            notes: $"Installment 1 — {notification.StartDate:MMMM yyyy}"
        );

        // First installment's due date is already in the past → the tenant paid in
        // cash / real-world before the lease was digitised.  Mark it automatically
        // so it never appears in the overdue queue and no late fee can be applied.
        if (isPast)
        {
            payment.MarkAsPaidExternally("First installment settled outside the platform (lease registered after due date).");
            _logger.LogInformation(
                "Lease {LeaseId}: first due date {DueDate} is in the past — marked as PaidExternally.",
                notification.LeaseId, firstDueDate);
        }

        await _repository.AddAsync(payment, cancellationToken);

        // Store the lease schedule so subsequent installments can be auto-created
        // when each payment is marked Paid (without crossing the module boundary).
        var config = LeaseScheduleConfig.Create(
            leaseId: notification.LeaseId,
            propertyId: notification.PropertyId,
            tenantId: notification.TenantId,
            landlordId: notification.OwnerId,
            installmentAmount: notification.InstallmentAmount,
            paymentFrequency: notification.PaymentFrequency,
            paymentDayOfMonth: notification.PaymentDayOfMonth,
            startDate: notification.StartDate,
            endDate: notification.EndDate
        );
        await _repository.SaveLeaseScheduleConfigAsync(config, cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created first payment record for lease {LeaseId} due {DueDate} | Status={Status}",
            notification.LeaseId, firstDueDate, payment.Status);
    }
}
