using Document.Application.Interfaces;
using MediatR;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;

namespace Document.Application.Commands.Lease.TerminateLease;

public class TerminateLeaseHandler : IRequestHandler<TerminateLeaseCommand>
{
    private readonly IDocumentDbContext _db;
    private readonly IPublisher _publisher;
    public TerminateLeaseHandler(IDocumentDbContext db, IPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task Handle(TerminateLeaseCommand cmd, CancellationToken ct)
    {
        var lease = await _db.Leases.FindAsync(new object[] { cmd.LeaseId }, ct)
            ?? throw new Exception("Lease not found");

        var wasActive = lease.IsActive;

        lease.IsActive = false;
        lease.TerminatedAt = DateTime.UtcNow;
        lease.TerminationReason = cmd.Reason;
        lease.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        // Only return the property to market if this lease was actually holding it.
        if (wasActive)
            await _publisher.Publish(new LeaseEndedEvent(lease.Id, lease.PropertyId), ct);
    }
}
