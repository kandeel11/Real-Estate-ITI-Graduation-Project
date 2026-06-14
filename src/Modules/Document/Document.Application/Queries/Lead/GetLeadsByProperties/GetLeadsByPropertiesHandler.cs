using Document.Application.Interfaces;
using Document.Application.Queries.Lead.GetLeadsByBroker;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Queries.Lead.GetLeadsByProperties;

public class GetLeadsByPropertiesHandler
    : IRequestHandler<GetLeadsByPropertiesQuery, List<LeadDto>>
{
    private readonly IDocumentDbContext _db;

    public GetLeadsByPropertiesHandler(IDocumentDbContext db) => _db = db;

    public async Task<List<LeadDto>> Handle(GetLeadsByPropertiesQuery query, CancellationToken ct)
    {
        if (query.PropertyIds.Count == 0) return [];

        var ids = query.PropertyIds.ToList();
        return await _db.Leads
            .Where(l => ids.Contains(l.PropertyId))
            .OrderByDescending(l => l.UpdatedAt)
            .Select(l => new LeadDto(
                l.Id, l.InquiryId, l.PropertyId,
                l.TenantId, l.OwnerId,
                l.Status.ToString(), l.DealValue,
                l.Notes, l.CreatedAt, l.UpdatedAt))
            .ToListAsync(ct);
    }
}
