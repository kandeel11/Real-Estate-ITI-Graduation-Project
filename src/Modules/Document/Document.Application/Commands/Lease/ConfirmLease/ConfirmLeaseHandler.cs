using Document.Application.Interfaces;
using Document.Domain.Enums;
using Document.Domain.Events;
using MediatR;

namespace Document.Application.Commands.Lease.ConfirmLease;

public class ConfirmLeaseHandler : IRequestHandler<ConfirmLeaseCommand, ConfirmLeaseResult>
{
    private readonly IDocumentDbContext _db;
    private readonly IPublisher _publisher;

    public ConfirmLeaseHandler(IDocumentDbContext db, IPublisher publisher)
    { _db = db; _publisher = publisher; }

    public async Task<ConfirmLeaseResult> Handle(ConfirmLeaseCommand cmd, CancellationToken ct)
    {
        var lease = await _db.Leases.FindAsync(new object[] { cmd.LeaseId }, ct)
            ?? throw new Exception("Lease not found");

        bool isOwner = cmd.ConfirmerId == lease.OwnerId;
        bool isTenant = cmd.ConfirmerId == lease.TenantId;

        if (!isOwner && !isTenant)
            throw new UnauthorizedAccessException("Only the owner or tenant can confirm this lease.");

        if (lease.ExtractionStatus == LeaseExtractionStatus.Rejected)
            throw new InvalidOperationException("This lease was rejected and cannot be confirmed.");

        if (isOwner)
        {
            lease.RentAmount = cmd.RentAmount;
            lease.SecurityDeposit = cmd.SecurityDeposit;
            lease.StartDate = cmd.StartDate;
            lease.EndDate = cmd.EndDate;
            lease.PaymentDayOfMonth = cmd.PaymentDayOfMonth;
            lease.OwnerConfirmedAt = DateTime.UtcNow;
        }
        else
        {
            lease.TenantConfirmedAt = DateTime.UtcNow;
        }

        lease.ConfirmedById = cmd.ConfirmerId;
        lease.ConfirmedAt = DateTime.UtcNow;
        lease.UpdatedAt = DateTime.UtcNow;

        bool bothConfirmed = lease.OwnerConfirmedAt.HasValue && lease.TenantConfirmedAt.HasValue;

        if (bothConfirmed && !lease.IsActive)
        {
            lease.IsActive = true;
            lease.ExtractionStatus = LeaseExtractionStatus.Confirmed;

            await _publisher.Publish(
                new LeaseActivatedEvent(
                    lease.Id,
                    lease.PropertyId,
                    lease.OwnerId,
                    lease.RentAmount ?? 0,
                    lease.PaymentDayOfMonth ?? 1,
                    lease.StartDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                    lease.EndDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
                    lease.ContractFileUrl
                ), ct);
        }

        await _db.SaveChangesAsync(ct);

        return new ConfirmLeaseResult(
            lease.IsActive,
            lease.OwnerConfirmedAt.HasValue,
            lease.TenantConfirmedAt.HasValue,
            lease.ExtractionStatus.ToString());
    }
}

public class RejectLeaseHandler : IRequestHandler<RejectLeaseCommand, RejectLeaseResult>
{
    private readonly IDocumentDbContext _db;
    private readonly INotificationService _notifications;

    public RejectLeaseHandler(IDocumentDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public async Task<RejectLeaseResult> Handle(RejectLeaseCommand cmd, CancellationToken ct)
    {
        var lease = await _db.Leases.FindAsync(new object[] { cmd.LeaseId }, ct)
            ?? throw new Exception("Lease not found");

        if (cmd.TenantId != lease.TenantId)
            throw new UnauthorizedAccessException("Only the tenant can reject this lease.");

        lease.ExtractionStatus = LeaseExtractionStatus.Rejected;
        lease.RejectionReason = cmd.Reason;
        lease.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        // إشعار الـ Owner
        await _notifications.CreateInAppAsync(
            userId: lease.OwnerId,
            title: "Lease Rejected",
            body: $"The tenant rejected the lease terms. Reason: {cmd.Reason}",
            type: NotificationType.LeaseRejected,
            relatedEntityType: "lease",
            relatedEntityId: lease.Id,
            ct: ct
        );

        // TODO: إشعار الأدمن (لو فيه IAdminNotificationService أو role-based notification)

        return new RejectLeaseResult(true, "Rejected");
    }
}