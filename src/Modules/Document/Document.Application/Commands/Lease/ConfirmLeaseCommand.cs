using Document.Application.Interfaces;
using Document.Domain.Enums;
using MediatR;

namespace Document.Application.Commands.Lease;

public record ConfirmLeaseCommand(Guid LeaseId, Guid OwnerId) : IRequest;

public class ConfirmLeaseHandler : IRequestHandler<ConfirmLeaseCommand>
{
    private readonly IDocumentDbContext _db;
    public ConfirmLeaseHandler(IDocumentDbContext db) => _db = db;

    public async Task Handle(ConfirmLeaseCommand cmd, CancellationToken ct)
    {
        var lease = await _db.Leases.FindAsync(new object[] { cmd.LeaseId }, ct)
            ?? throw new InvalidOperationException("Lease not found");

        lease.IsActive = true;
        lease.ExtractionStatus = LeaseExtractionStatus.Confirmed;
        lease.ConfirmedById = cmd.OwnerId;
        lease.ConfirmedAt = DateTime.UtcNow;
        lease.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }
}