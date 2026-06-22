using Document.Application.Commands.Lease;
using Document.Application.Interfaces;
using Document.Domain.Enums;
using Identity.Application.Interfaces;
using MediatR;

namespace Document.Application.Commands.Lease.RequestRenewal;

public class RequestLeaseRenewalHandler : IRequestHandler<RequestLeaseRenewalCommand>
{
    private readonly IDocumentDbContext _db;
    private readonly INotificationService _notifications;
    private readonly IUserRepository _users;

    public RequestLeaseRenewalHandler(
        IDocumentDbContext db, INotificationService notifications, IUserRepository users)
    {
        _db = db;
        _notifications = notifications;
        _users = users;
    }

    public async Task Handle(RequestLeaseRenewalCommand cmd, CancellationToken ct)
    {
        var lease = await _db.Leases.FindAsync(new object[] { cmd.LeaseId }, ct)
            ?? throw new KeyNotFoundException("Lease not found");

        if (lease.TenantId != cmd.TenantId)
            throw new UnauthorizedAccessException("Only the tenant can request a renewal.");

        if (!lease.IsActive || lease.TerminatedAt is not null)
            throw new InvalidOperationException("Only an active lease can be renewed.");

        if (lease.RenewalRequestedAt is not null)
            throw new InvalidOperationException("A renewal request is already pending for this lease.");

        lease.RenewalRequestedAt = DateTime.UtcNow;
        lease.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        var tenantName = (await _users.GetByIdAsync(cmd.TenantId, ct))?.FullName ?? "The tenant";
        var body = $"{tenantName} requested to renew their lease. Upload a new contract to send renewed terms.";

        foreach (var recipient in LeaseParties.CounterpartiesOf(lease, cmd.TenantId))
            await _notifications.CreateInAppAsync(
                userId: recipient,
                title: "Lease Renewal Requested",
                body: body,
                type: NotificationType.LeaseRenewalRequested,
                relatedEntityType: "lease",
                relatedEntityId: lease.Id,
                ct: ct);
    }
}
