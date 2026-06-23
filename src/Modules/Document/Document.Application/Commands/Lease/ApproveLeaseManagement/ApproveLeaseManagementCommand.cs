using Document.Application.Interfaces;
using Document.Domain.Enums;
using MediatR;

namespace Document.Application.Commands.Lease.ApproveLeaseManagement;

public record ApproveLeaseManagementCommand(
    Guid LeaseId,
    Guid ManagerId,    // UserId of the approving company manager
    bool IsApproved,
    string? Notes
) : IRequest;

public sealed class ApproveLeaseManagementHandler : IRequestHandler<ApproveLeaseManagementCommand>
{
    private readonly IDocumentDbContext _db;
    private readonly INotificationService _notifications;

    public ApproveLeaseManagementHandler(IDocumentDbContext db, INotificationService notifications)
    { _db = db; _notifications = notifications; }

    public async Task Handle(ApproveLeaseManagementCommand cmd, CancellationToken ct)
    {
        var lease = await _db.Leases.FindAsync(new object[] { cmd.LeaseId }, ct)
            ?? throw new KeyNotFoundException($"Lease {cmd.LeaseId} not found.");

        if (lease.ManagerApprovalStatus != "PendingApproval")
            throw new InvalidOperationException("This lease is not awaiting manager approval.");

        lease.ManagerId = cmd.ManagerId;
        lease.ManagerNotes = cmd.Notes;
        lease.UpdatedAt = DateTime.UtcNow;

        if (cmd.IsApproved)
        {
            lease.ManagerApprovalStatus = "Approved";
            lease.ManagerApprovedAt = DateTime.UtcNow;

            // Notify the broker that they can now send to tenant
            var notifyId = lease.BrokerId ?? lease.OwnerId;
            await _notifications.CreateInAppAsync(
                userId: notifyId,
                title: "Management Options Approved — Ready for Tenant",
                body: "The company manager has approved the management options. You can now send the lease to the tenant.",
                type: NotificationType.ContractSigned,
                relatedEntityType: "lease",
                relatedEntityId: cmd.LeaseId,
                ct: ct);
        }
        else
        {
            lease.ManagerApprovalStatus = "Rejected";
            lease.ManagerRejectedAt = DateTime.UtcNow;

            var notifyId = lease.BrokerId ?? lease.OwnerId;
            await _notifications.CreateInAppAsync(
                userId: notifyId,
                title: "Management Options Rejected",
                body: $"The company manager rejected the management options. {(string.IsNullOrEmpty(cmd.Notes) ? "" : $"Reason: {cmd.Notes}")}",
                type: NotificationType.LeaseRejected,
                relatedEntityType: "lease",
                relatedEntityId: cmd.LeaseId,
                ct: ct);
        }

        await _db.SaveChangesAsync(ct);
    }
}
