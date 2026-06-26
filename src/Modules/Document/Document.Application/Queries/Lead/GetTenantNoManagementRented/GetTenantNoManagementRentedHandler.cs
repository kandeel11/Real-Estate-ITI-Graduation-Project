using Document.Application.Interfaces;
using Document.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Queries.Lead.GetTenantNoManagementRented;

public class GetTenantNoManagementRentedHandler
    : IRequestHandler<GetTenantNoManagementRentedQuery, List<Guid>>
{
    private readonly IDocumentDbContext _db;

    public GetTenantNoManagementRentedHandler(IDocumentDbContext db) => _db = db;

    public async Task<List<Guid>> Handle(GetTenantNoManagementRentedQuery query, CancellationToken ct)
    {
        // ClosedWon leads for this tenant that have no associated lease.
        var closedLeadIds = await _db.Leads
            .Where(l => l.TenantId == query.TenantId && l.Status == LeadStatus.ClosedWon)
            .Select(l => new { l.Id, l.PropertyId })
            .ToListAsync(ct);

        if (closedLeadIds.Count == 0) return [];

        // Lead IDs that DO have a lease (full-management path — already in the tenant's lease list).
        var leaseLeadIds = await _db.Leases
            .Where(l => l.LeadId != null && closedLeadIds.Select(x => x.Id).Contains(l.LeadId!.Value))
            .Select(l => l.LeadId!.Value)
            .Distinct()
            .ToListAsync(ct);

        return closedLeadIds
            .Where(l => !leaseLeadIds.Contains(l.Id))
            .Select(l => l.PropertyId)
            .Distinct()
            .ToList();
    }
}
