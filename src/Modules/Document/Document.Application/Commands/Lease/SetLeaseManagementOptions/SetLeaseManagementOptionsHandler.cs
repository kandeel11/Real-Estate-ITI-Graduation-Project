using Document.Application.Interfaces;
using Document.Domain.Enums;
using MediatR;

namespace Document.Application.Commands.Lease.SetLeaseManagementOptions;

public sealed class SetLeaseManagementOptionsHandler
    : IRequestHandler<SetLeaseManagementOptionsCommand, SetLeaseManagementOptionsResult>
{
    private readonly IDocumentDbContext _db;
    private readonly INotificationService _notifications;

    public SetLeaseManagementOptionsHandler(IDocumentDbContext db, INotificationService notifications)
    { _db = db; _notifications = notifications; }

    public async Task<SetLeaseManagementOptionsResult> Handle(
        SetLeaseManagementOptionsCommand cmd, CancellationToken ct)
    {
        var lease = await _db.Leases.FindAsync(new object[] { cmd.LeaseId }, ct)
            ?? throw new KeyNotFoundException($"Lease {cmd.LeaseId} not found.");

        var selected = new HashSet<string>(cmd.ManagementOptions, StringComparer.OrdinalIgnoreCase);
        // Full management = both Maintenance AND PaymentCollection selected
        bool isFullManagement = selected.Contains("Maintenance") && selected.Contains("PaymentCollection");

        lease.ManagementOptions = System.Text.Json.JsonSerializer.Serialize(cmd.ManagementOptions);
        lease.IsFullManagement = isFullManagement;
        // Full management goes straight to tenant; partial management needs manager sign-off first
        lease.ManagerApprovalStatus = isFullManagement ? null : "PendingApproval";
        lease.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        if (!isFullManagement && cmd.ManagerUserId.HasValue)
        {
            var optionsList = string.Join(", ", cmd.ManagementOptions);
            await _notifications.CreateInAppAsync(
                userId: cmd.ManagerUserId.Value,
                title: "Management Options Need Your Approval",
                body: $"A broker selected partial management options [{optionsList}] for a lease and requires your approval before it proceeds to the tenant.",
                type: NotificationType.ContractSigned,
                relatedEntityType: "lease",
                relatedEntityId: cmd.LeaseId,
                ct: ct);
        }

        return new SetLeaseManagementOptionsResult(
            isFullManagement,
            !isFullManagement,
            isFullManagement ? "NotRequired" : "PendingApproval");
    }
}
