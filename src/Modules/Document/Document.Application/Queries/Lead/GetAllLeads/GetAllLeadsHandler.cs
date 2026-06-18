using Document.Application.Interfaces;
using Document.Application.Queries.Lead.GetLeadsByBroker;
using Document.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Queries.Lead.GetAllLeads;

public class GetAllLeadsHandler : IRequestHandler<GetAllLeadsQuery, List<LeadDto>>
{
    private readonly IDocumentDbContext _db;

    public GetAllLeadsHandler(IDocumentDbContext db) => _db = db;

    public async Task<List<LeadDto>> Handle(GetAllLeadsQuery query, CancellationToken ct)
    {
        var q = _db.Leads.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<LeadStatus>(query.Status, ignoreCase: true, out var status))
        {
            q = q.Where(l => l.Status == status);
        }

        return await q
            .OrderByDescending(l => l.UpdatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(l => new LeadDto(
                l.Id, l.InquiryId, l.PropertyId,
                l.TenantId, l.OwnerId,
                l.Status.ToString(), l.DealValue,
                l.Notes, l.CreatedAt, l.UpdatedAt))
            .ToListAsync(ct);
    }
}
