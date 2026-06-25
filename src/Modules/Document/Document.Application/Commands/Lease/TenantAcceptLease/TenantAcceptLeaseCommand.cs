using Document.Application.Interfaces;
using Document.Domain.Enums;
using Document.Domain.Events;
using MediatR;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;

namespace Document.Application.Commands.Lease.TenantAcceptLease;

public record TenantAcceptLeaseCommand(
    Guid LeaseId,
    Guid TenantId
) : IRequest;

public sealed class TenantAcceptLeaseHandler : IRequestHandler<TenantAcceptLeaseCommand>
{
    private readonly IDocumentDbContext _db;
    private readonly IPublisher _publisher;
    private readonly INotificationService _notifications;

    public TenantAcceptLeaseHandler(IDocumentDbContext db, IPublisher publisher, INotificationService notifications)
    {
        _db = db;
        _publisher = publisher;
        _notifications = notifications;
    }

    public async Task Handle(TenantAcceptLeaseCommand cmd, CancellationToken ct)
    {
        var lease = await _db.Leases.FindAsync(new object[] { cmd.LeaseId }, ct)
            ?? throw new KeyNotFoundException($"Lease {cmd.LeaseId} not found.");

        if (lease.TenantId != cmd.TenantId)
            throw new UnauthorizedAccessException("Only the assigned tenant can accept this lease.");

        if (lease.ExtractionStatus != LeaseExtractionStatus.Extracted)
            throw new InvalidOperationException("This lease is not currently awaiting tenant acceptance.");

        if (lease.IsActive)
            throw new InvalidOperationException("This lease is already active.");

        lease.TenantConfirmedAt = DateTime.UtcNow;
        lease.TenantSignedAt = DateTime.UtcNow;
        lease.IsActive = true;
        lease.ExtractionStatus = LeaseExtractionStatus.Confirmed;
        lease.UpdatedAt = DateTime.UtcNow;

        var isMaintenanceEnabled = lease.IsFullManagement ||
            (lease.ManagementOptions?.Contains("Maintenance", StringComparison.OrdinalIgnoreCase) ?? false);
        var isPaymentEnabled = lease.IsFullManagement ||
            (lease.ManagementOptions?.Contains("Payment", StringComparison.OrdinalIgnoreCase) ?? false);

        await _publisher.Publish(
            new LeaseActivatedEvent(
                lease.Id,
                lease.PropertyId,
                lease.OwnerId,
                lease.InstallmentAmount ?? 0,
                lease.PaymentDayOfMonth ?? 1,
                lease.StartDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                lease.EndDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
                lease.ContractFileUrl,
                lease.IsFullManagement,
                isMaintenanceEnabled,
                isPaymentEnabled
            ), ct);

        await _publisher.Publish(
            new LeaseActivatedIntegrationEvent(
                LeaseId: lease.Id,
                PropertyId: lease.PropertyId,
                OwnerId: lease.OwnerId,
                TenantId: lease.TenantId,
                InstallmentAmount: lease.InstallmentAmount ?? 0,
                PaymentFrequency: lease.PaymentFrequency?.ToString() ?? "Monthly",
                DurationInMonths: lease.DurationInMonths ?? 12,
                PaymentDayOfMonth: lease.PaymentDayOfMonth ?? 1,
                StartDate: lease.StartDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate: lease.EndDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1))
            ), ct);

        await _db.SaveChangesAsync(ct);

        var notifyId = lease.BrokerId ?? lease.OwnerId;
        await _notifications.CreateInAppAsync(
            userId: notifyId,
            title: "Tenant Accepted the Lease",
            body: "The tenant has accepted and signed the lease agreement. The lease is now active.",
            type: NotificationType.ContractSigned,
            relatedEntityType: "lease",
            relatedEntityId: lease.Id,
            ct: ct);
    }
}
