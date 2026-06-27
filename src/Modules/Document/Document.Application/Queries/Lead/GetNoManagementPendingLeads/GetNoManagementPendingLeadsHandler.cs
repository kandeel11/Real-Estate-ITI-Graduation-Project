using Document.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Queries.Lead.GetNoManagementPendingLeads;

public sealed class GetNoManagementPendingLeadsHandler
    : IRequestHandler<GetNoManagementPendingLeadsQuery, List<NoManagementPendingLeadDto>>
{
    private readonly IDocumentDbContext _db;

    public GetNoManagementPendingLeadsHandler(IDocumentDbContext db) => _db = db;

    public async Task<List<NoManagementPendingLeadDto>> Handle(
        GetNoManagementPendingLeadsQuery request, CancellationToken ct)
        => await _db.Leads
            .Where(l => l.NoManagementManagerId == request.ManagerUserId
                     && l.NoManagementPendingAt != null)
            .OrderBy(l => l.NoManagementPendingAt)
            .Select(l => new NoManagementPendingLeadDto(
                l.Id,
                l.PropertyId,
                l.TenantId,
                l.OwnerId,
                l.DealValue,
                l.NoManagementPendingAt!.Value))
            .ToListAsync(ct);
}
