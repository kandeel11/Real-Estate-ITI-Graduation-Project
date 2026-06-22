using Document.Application.Interfaces;
using Document.Application.Queries.Lease.GetLeaseById;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Queries.Lease;

public record GetMyLeasesQuery(Guid TenantId) : IRequest<List<LeaseDto>>;

public class GetMyLeasesHandler(IDocumentDbContext db)
    : IRequestHandler<GetMyLeasesQuery, List<LeaseDto>>
{
    public async Task<List<LeaseDto>> Handle(GetMyLeasesQuery q, CancellationToken ct)
    {
        var leases = await db.Leases
            .Where(l => l.TenantId == q.TenantId && l.DeletedAt == null)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(ct);

        return leases.Select(LeaseDto.FromEntity).ToList();
    }
}