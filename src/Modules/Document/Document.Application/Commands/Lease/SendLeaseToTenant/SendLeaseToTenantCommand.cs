using Document.Application.Interfaces;
using Document.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Commands.Lease.SendLeaseToTenant;

/// <summary>
/// Persists the broker's reviewed/edited lease terms and notifies the tenant that a
/// lease is ready for their review.
/// </summary>
public record SendLeaseToTenantCommand(
    Guid LeaseId,
    decimal? InstallmentAmount,
    decimal? Deposit,
    DateOnly? StartDate,
    DateOnly? EndDate) : IRequest;

public class SendLeaseToTenantCommandHandler : IRequestHandler<SendLeaseToTenantCommand>
{
    private readonly IDocumentDbContext _db;
    private readonly INotificationService _notifications;

    public SendLeaseToTenantCommandHandler(IDocumentDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public async Task Handle(SendLeaseToTenantCommand cmd, CancellationToken ct)
    {
        var lease = await _db.Leases
            .FirstOrDefaultAsync(l => l.Id == cmd.LeaseId && l.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException($"Lease {cmd.LeaseId} not found.");

        if (cmd.InstallmentAmount.HasValue) lease.InstallmentAmount = cmd.InstallmentAmount;
        if (cmd.Deposit.HasValue) lease.SecurityDeposit = cmd.Deposit;
        if (cmd.StartDate.HasValue) lease.StartDate = cmd.StartDate;
        if (cmd.EndDate.HasValue) lease.EndDate = cmd.EndDate;

        // Sending to the tenant is the owner/broker side finalising the terms:
        //  • Mark the owner side confirmed (the tenant's later accept completes the pair → lease activates).
        //  • Set status to "extracted" so it surfaces in the tenant's pending-offer query.
        lease.OwnerConfirmedAt ??= DateTime.UtcNow;
        lease.ExtractionStatus = LeaseExtractionStatus.Extracted;
        lease.RejectionReason = null;
        lease.UpdatedAt = DateTime.UtcNow;

        // If this is a renewal being offered, the predecessor's pending renewal request is
        // now fulfilled — clear it here (not on upload) so a mid-flow reload keeps the
        // "renewal requested" state until the offer actually reaches the tenant.
        if (lease.RenewedFromLeaseId is { } prevId)
        {
            var predecessor = await _db.Leases
                .FirstOrDefaultAsync(l => l.Id == prevId && l.DeletedAt == null, ct);
            if (predecessor?.RenewalRequestedAt is not null)
            {
                predecessor.RenewalRequestedAt = null;
                predecessor.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync(ct);

        await _notifications.CreateInAppAsync(
            userId: lease.TenantId,
            title: "Lease Ready for Review",
            body: "Your landlord has sent a lease agreement for your review and signature.",
            type: NotificationType.ContractSigned,
            relatedEntityType: "lease",
            relatedEntityId: lease.Id,
            ct: ct);
    }
}
