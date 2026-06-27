using Document.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Queries.Lead.GetLeadsByBroker;

public class GetLeadsByBrokerHandler 
    : IRequestHandler<GetLeadsByBrokerQuery, List<LeadDto>>
{
    private readonly IDocumentDbContext _db;

    public GetLeadsByBrokerHandler(IDocumentDbContext db) => _db = db;

    public async Task<List<LeadDto>> Handle(
        GetLeadsByBrokerQuery query, CancellationToken ct)
    {
        // بنجيب الـ InquiryIds المسندة للـ broker
        var assignedInquiryIds = await _db.InquiryAssignments
            .Where(a => a.AssignedToId == query.BrokerId)
            .Select(a => a.InquiryId)
            .ToListAsync(ct);

        return await _db.Leads
            .Where(l => assignedInquiryIds.Contains(l.InquiryId))
            .OrderByDescending(l => l.UpdatedAt)
            .Select(l => new LeadDto(
                l.Id, l.InquiryId, l.PropertyId,
                l.TenantId, l.OwnerId,
                l.Status.ToString(), l.DealValue,
                l.Notes, l.CreatedAt, l.UpdatedAt,
                l.NoManagementPendingAt, l.NoManagementManagerId))
            .ToListAsync(ct);
    }
}