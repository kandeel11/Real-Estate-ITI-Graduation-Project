using Document.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Queries.Lease.GetLeadsWithLeases;

/// <summary>
/// Returns the subset of the given lead ids that already have a (non-deleted) lease.
/// Lets the broker pipeline show "Lease Created" instead of "Create Lease".
/// </summary>
public record GetLeadsWithLeasesQuery(IReadOnlyCollection<Guid> LeadIds) : IRequest<HashSet<Guid>>;

public class GetLeadsWithLeasesHandler : IRequestHandler<GetLeadsWithLeasesQuery, HashSet<Guid>>
{
    private readonly IDocumentDbContext _db;

    public GetLeadsWithLeasesHandler(IDocumentDbContext db) => _db = db;

    public async Task<HashSet<Guid>> Handle(GetLeadsWithLeasesQuery query, CancellationToken ct)
    {
        if (query.LeadIds.Count == 0)
            return new HashSet<Guid>();

        var ids = await _db.Leases
            .Where(l => l.LeadId != null && query.LeadIds.Contains(l.LeadId.Value) && l.DeletedAt == null)
            .Select(l => l.LeadId!.Value)
            .Distinct()
            .ToListAsync(ct);

        return ids.ToHashSet();
    }
}
