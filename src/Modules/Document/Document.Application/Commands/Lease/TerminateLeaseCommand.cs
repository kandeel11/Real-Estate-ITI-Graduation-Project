using Document.Application.Interfaces;
using MediatR;

namespace Document.Application.Commands.Lease;

public record TerminateLeaseCommand(Guid LeaseId, string Reason) : IRequest;

public class TerminateLeaseHandler : IRequestHandler<TerminateLeaseCommand>
{
    private readonly IDocumentDbContext _db;
    public TerminateLeaseHandler(IDocumentDbContext db) => _db = db;

    public async Task Handle(TerminateLeaseCommand cmd, CancellationToken ct)
    {
        var lease = await _db.Leases.FindAsync(new object[] { cmd.LeaseId }, ct)
            ?? throw new InvalidOperationException("Lease not found");

        lease.IsActive = false;
        lease.TerminatedAt = DateTime.UtcNow;
        lease.TerminationReason = cmd.Reason;
        lease.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }
}