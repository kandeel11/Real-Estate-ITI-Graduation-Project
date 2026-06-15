using Document.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;

namespace Document.Application.Commands.Lease.ExpireDueLeases;

/// <summary>
/// Sweeps active leases whose EndDate has passed: marks them inactive and publishes
/// LeaseEndedEvent for each so the Property module returns the listing to market.
/// Idempotent — only touches still-active, non-deleted leases. Returns how many expired.
/// </summary>
public record ExpireDueLeasesCommand : IRequest<int>;

public class ExpireDueLeasesHandler : IRequestHandler<ExpireDueLeasesCommand, int>
{
    private readonly IDocumentDbContext _db;
    private readonly IPublisher _publisher;

    public ExpireDueLeasesHandler(IDocumentDbContext db, IPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task<int> Handle(ExpireDueLeasesCommand request, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var due = await _db.Leases
            .Where(l => l.IsActive
                     && l.DeletedAt == null
                     && l.EndDate != null
                     && l.EndDate < today)
            .ToListAsync(ct);

        if (due.Count == 0) return 0;

        var now = DateTime.UtcNow;
        foreach (var lease in due)
        {
            lease.IsActive = false;
            lease.UpdatedAt = now;
        }

        await _db.SaveChangesAsync(ct);

        foreach (var lease in due)
            await _publisher.Publish(new LeaseEndedEvent(lease.Id, lease.PropertyId), ct);

        return due.Count;
    }
}
