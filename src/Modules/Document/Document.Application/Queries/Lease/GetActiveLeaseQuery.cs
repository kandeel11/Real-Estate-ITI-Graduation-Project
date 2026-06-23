using Document.Application.Interfaces;
using Document.Application.Queries.Lease.GetLeaseById;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Queries.Lease;

public record GetActiveLeaseQuery(Guid TenantId) : IRequest<LeaseDto?>;

public class GetActiveLeaseHandler(IDocumentDbContext db)
    : IRequestHandler<GetActiveLeaseQuery, LeaseDto?>
{
    public async Task<LeaseDto?> Handle(GetActiveLeaseQuery q, CancellationToken ct)
    {
        var l = await db.Leases
            .Where(x => x.TenantId == q.TenantId && x.IsActive && x.DeletedAt == null)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (l is null) return null;
        return LeaseDto.FromEntity(l);
    }
}