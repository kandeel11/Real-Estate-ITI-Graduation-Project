using Document.Application.Commands.Lease;
using Document.Application.Interfaces;
using Document.Domain.Enums;
using Identity.Application.Interfaces;
using MediatR;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;

namespace Document.Application.Commands.Lease.RespondToTermination;

public class RespondToLeaseTerminationHandler : IRequestHandler<RespondToLeaseTerminationCommand>
{
    private readonly IDocumentDbContext _db;
    private readonly IPublisher _publisher;
    private readonly INotificationService _notifications;
    private readonly IUserRepository _users;

    public RespondToLeaseTerminationHandler(
        IDocumentDbContext db, IPublisher publisher,
        INotificationService notifications, IUserRepository users)
    {
        _db = db;
        _publisher = publisher;
        _notifications = notifications;
        _users = users;
    }

    public async Task Handle(RespondToLeaseTerminationCommand cmd, CancellationToken ct)
    {
        var lease = await _db.Leases.FindAsync(new object[] { cmd.LeaseId }, ct)
            ?? throw new KeyNotFoundException("Lease not found");

        if (lease.TerminationRequestedAt is null || lease.TerminationRequestedById is not { } requesterId)
            throw new InvalidOperationException("There is no pending termination request for this lease.");

        if (!LeaseParties.IsCounterparty(lease, requesterId, cmd.ResponderId))
            throw new UnauthorizedAccessException("Only the other party can respond to this termination request.");

        var responderName = (await _users.GetByIdAsync(cmd.ResponderId, ct))?.FullName ?? "The other party";

        if (cmd.Approve)
        {
            var wasActive = lease.IsActive;

            lease.IsActive = false;
            lease.TerminatedAt = DateTime.UtcNow;
            // TerminationReason was set with the request; keep it.
            lease.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            // Return the property to market / restore the usage slot only if it was holding it.
            if (wasActive)
                await _publisher.Publish(new LeaseEndedEvent(lease.Id, lease.PropertyId), ct);

            await _notifications.CreateInAppAsync(
                userId: requesterId,
                title: "Lease Termination Approved",
                body: $"{responderName} approved ending the lease. The lease is now terminated.",
                type: NotificationType.LeaseTerminationApproved,
                relatedEntityType: "lease",
                relatedEntityId: lease.Id,
                ct: ct);
        }
        else
        {
            lease.TerminationRequestedById = null;
            lease.TerminationRequestedAt = null;
            lease.TerminationEffectiveDate = null;
            lease.TerminationReason = null;
            lease.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            var note = string.IsNullOrWhiteSpace(cmd.ResponseNote) ? string.Empty : $" Note: {cmd.ResponseNote}";
            await _notifications.CreateInAppAsync(
                userId: requesterId,
                title: "Lease Termination Declined",
                body: $"{responderName} declined the termination request. The lease continues.{note}",
                type: NotificationType.LeaseTerminationRejected,
                relatedEntityType: "lease",
                relatedEntityId: lease.Id,
                ct: ct);
        }
    }
}
