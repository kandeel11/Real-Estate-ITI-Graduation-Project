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
            "Lease {LeaseId} activated — generating first installment ({Frequency})",
            notification.LeaseId, notification.PaymentFrequency);

        int day = Math.Min(notification.PaymentDayOfMonth,
            DateTime.DaysInMonth(notification.StartDate.Year, notification.StartDate.Month));
        var firstDueDate = new DateOnly(notification.StartDate.Year, notification.StartDate.Month, day);

        bool exists = await _repository.ExistsForLeaseAndMonthAsync(
            notification.LeaseId, firstDueDate, cancellationToken);

        if (exists)
        {
            _logger.LogInformation(
                "First payment already exists for lease {LeaseId} due {DueDate} — skipping",
                notification.LeaseId, firstDueDate);
            return;
        }

        var payment = PaymentEntity.CreateRent(
            leaseId: notification.LeaseId,
            propertyId: notification.PropertyId,
            tenantId: notification.TenantId,
            landlordId: notification.OwnerId,
            amount: notification.InstallmentAmount,
            dueDate: firstDueDate,
            notes: $"Installment 1 — {notification.StartDate:MMMM yyyy}"
        );

        await _repository.AddAsync(payment, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created first payment record for lease {LeaseId} due {DueDate}",
            notification.LeaseId, firstDueDate);
    }
}
