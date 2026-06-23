using Document.Application.Interfaces;
using Document.Application.Queries.Lease.GetLeaseById;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Queries.Lease;

public record GetLeasesByOwnerQuery(Guid OwnerId) : IRequest<List<LeaseDto>>;

public class GetLeasesByOwnerHandler(IDocumentDbContext db)
    : IRequestHandler<GetLeasesByOwnerQuery, List<LeaseDto>>
{
    public async Task<List<LeaseDto>> Handle(GetLeasesByOwnerQuery q, CancellationToken ct)
    {
        var leases = await db.Leases
            .Where(l => l.OwnerId == q.OwnerId && l.DeletedAt == null)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(ct);

        return leases.Select(LeaseDto.FromEntity).ToList();
    }
}
