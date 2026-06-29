using MediatR;
using Microsoft.Extensions.Logging;
using Payment.Application.Interfaces;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;
using PaymentEntity = Payment.Domain.Entities.Payment;

namespace Payment.Application.Features.EventHandlers;

/// <summary>
/// When a rent payment is marked Paid (or PaidExternally), creates the next installment
/// automatically based on the stored LeaseScheduleConfig — no manual "Generate Monthly" needed.
/// </summary>
public sealed class RentPaymentPaidHandler
    : INotificationHandler<RentPaymentPaidIntegrationEvent>
{
    private readonly IPaymentRepository _repository;
    private readonly ILogger<RentPaymentPaidHandler> _logger;

    public RentPaymentPaidHandler(IPaymentRepository repository, ILogger<RentPaymentPaidHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Handle(RentPaymentPaidIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var config = await _repository.GetLeaseScheduleConfigAsync(notification.LeaseId, cancellationToken);
        if (config is null)
        {
            _logger.LogWarning(
                "RentPaymentPaid: no LeaseScheduleConfig found for lease {LeaseId} — cannot auto-create next installment.",
                notification.LeaseId);
            return;
        }

        var nextDueDate = config.NextDueDate(notification.PaidDueDate);
        if (nextDueDate is null)
        {
            _logger.LogInformation(
                "Lease {LeaseId}: no further installments after {DueDate} — lease ends {EndDate}.",
                notification.LeaseId, notification.PaidDueDate, config.EndDate);
            return;
        }

        bool alreadyExists = await _repository.ExistsForLeaseAndMonthAsync(
            notification.LeaseId, nextDueDate.Value, cancellationToken);

        if (alreadyExists)
        {
            _logger.LogInformation(
                "Lease {LeaseId}: installment for {Month}/{Year} already exists — skipping.",
                notification.LeaseId, nextDueDate.Value.Month, nextDueDate.Value.Year);
            return;
        }

        var installmentNumber = await CountInstallmentsAsync(notification.LeaseId, cancellationToken) + 1;

        var next = PaymentEntity.CreateRent(
            leaseId: config.LeaseId,
            propertyId: config.PropertyId,
            tenantId: config.TenantId,
            landlordId: config.LandlordId,
            amount: config.InstallmentAmount,
            dueDate: nextDueDate.Value,
            notes: $"Installment {installmentNumber} — {nextDueDate.Value:MMMM yyyy}"
        );

        await _repository.AddAsync(next, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Auto-created installment {Number} for lease {LeaseId} due {DueDate}.",
            installmentNumber, config.LeaseId, nextDueDate.Value);
    }

    private async Task<int> CountInstallmentsAsync(Guid leaseId, CancellationToken ct)
    {
        var all = await _repository.GetByLeaseIdAsync(leaseId, ct);
        return all.Count;
    }
}
