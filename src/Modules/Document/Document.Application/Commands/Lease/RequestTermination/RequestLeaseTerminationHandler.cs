using Document.Application.Interfaces;
using Document.Domain.Enums;
using Identity.Application.Interfaces;
using MediatR;

namespace Document.Application.Commands.Lease.RequestTermination;

public class RequestLeaseTerminationHandler : IRequestHandler<RequestLeaseTerminationCommand>
{
    private readonly IDocumentDbContext _db;
    private readonly INotificationService _notifications;
    private readonly IUserRepository _users;

    public RequestLeaseTerminationHandler(
        IDocumentDbContext db, INotificationService notifications, IUserRepository users)
    {
        _db = db;
        _notifications = notifications;
        _users = users;
    }

    public async Task Handle(RequestLeaseTerminationCommand cmd, CancellationToken ct)
    {
        var lease = await _db.Leases.FindAsync(new object[] { cmd.LeaseId }, ct)
            ?? throw new KeyNotFoundException("Lease not found");

        if (!lease.IsActive || lease.TerminatedAt is not null)
            throw new InvalidOperationException("Only an active lease can be terminated.");

        if (!LeaseParties.IsParty(lease, cmd.RequesterId))
            throw new UnauthorizedAccessException("Only a party to this lease can request termination.");

        if (lease.TerminationRequestedAt is not null)
            throw new InvalidOperationException("A termination request is already pending for this lease.");

        lease.TerminationRequestedById = cmd.RequesterId;
        lease.TerminationRequestedAt = DateTime.UtcNow;
        lease.TerminationEffectiveDate = cmd.EffectiveDate;
        lease.TerminationReason = cmd.Reason;
        lease.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        // Notify the OTHER side. If the tenant asked, notify owner (+ broker); else notify tenant.
        var requesterName = (await _users.GetByIdAsync(cmd.RequesterId, ct))?.FullName ?? "The other party";
        var whenText = cmd.EffectiveDate is { } d ? $" effective {d:yyyy-MM-dd}" : string.Empty;
        var body = $"{requesterName} requested to end the lease early{whenText}. Reason: {cmd.Reason}";

        foreach (var recipient in LeaseParties.CounterpartiesOf(lease, cmd.RequesterId))
            await _notifications.CreateInAppAsync(
                userId: recipient,
                title: "Lease Termination Requested",
                body: body,
                type: NotificationType.LeaseTerminationRequested,
                relatedEntityType: "lease",
                relatedEntityId: lease.Id,
                ct: ct);
    }
}
