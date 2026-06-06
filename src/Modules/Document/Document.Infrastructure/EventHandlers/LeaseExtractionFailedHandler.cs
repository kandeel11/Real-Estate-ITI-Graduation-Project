using Document.Application.Events;
using Document.Domain.Enums;
using Document.Infrastructure.Persistence;
using MediatR;

namespace Document.Infrastructure.EventHandlers;

public class LeaseExtractionFailedHandler : INotificationHandler<LeaseExtractionFailedEvent>
{
    private readonly DocumentDbContext _db;

    public LeaseExtractionFailedHandler(DocumentDbContext db) => _db = db;

    public async Task Handle(LeaseExtractionFailedEvent evt, CancellationToken ct)
    {
        var lease = await _db.Leases.FindAsync(new object[] { evt.LeaseId }, ct);
        if (lease == null) return;

        lease.ExtractionStatus = LeaseExtractionStatus.Failed;
        lease.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }
}