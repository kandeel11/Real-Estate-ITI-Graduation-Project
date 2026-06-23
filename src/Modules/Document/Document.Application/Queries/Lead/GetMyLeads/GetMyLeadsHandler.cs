using Document.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Queries.Lead.GetMyLeads;

public class GetMyLeadsHandler : IRequestHandler<GetMyLeadsQuery, List<TenantLeadDto>>
{
    private readonly IDocumentDbContext _db;

    public GetMyLeadsHandler(IDocumentDbContext db) => _db = db;

    public async Task<List<TenantLeadDto>> Handle(GetMyLeadsQuery query, CancellationToken ct)
    {
        return await _db.Leads
            .Where(l => l.TenantId == query.TenantId)
            .Join(
                _db.Inquiries,
                lead => lead.InquiryId,
                inquiry => inquiry.Id,
                (lead, inquiry) => new TenantLeadDto(
                    lead.Id,
                    lead.InquiryId,
                    lead.PropertyId,
                    lead.Status.ToString(),
                    inquiry.Message,
                    lead.Notes,
                    lead.DealValue,
                    lead.CreatedAt,
                    lead.UpdatedAt,
                    lead.ClosedAt))
            .OrderByDescending(l => l.UpdatedAt)
            .ToListAsync(ct);
    }
}
