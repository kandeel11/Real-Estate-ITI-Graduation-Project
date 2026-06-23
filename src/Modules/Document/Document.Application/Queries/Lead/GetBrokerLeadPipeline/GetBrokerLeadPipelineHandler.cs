using Document.Application.Interfaces;
using Document.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Queries.Lead.GetBrokerLeadPipeline;

public sealed class GetBrokerLeadPipelineHandler
    : IRequestHandler<GetBrokerLeadPipelineQuery, List<BrokerLeadPipelineDto>>
{
    private readonly IDocumentDbContext _db;

    public GetBrokerLeadPipelineHandler(IDocumentDbContext db) => _db = db;

    public async Task<List<BrokerLeadPipelineDto>> Handle(
        GetBrokerLeadPipelineQuery query, CancellationToken ct)
    {
        if (query.BrokerUserIds.Count == 0)
            return [];

        var assignments = await _db.InquiryAssignments
            .Where(a => query.BrokerUserIds.Contains(a.AssignedToId))
            .Select(a => new { a.AssignedToId, a.InquiryId })
            .ToListAsync(ct);

        var allInquiryIds = assignments.Select(a => a.InquiryId).Distinct().ToList();

        var leads = await _db.Leads
            .Where(l => allInquiryIds.Contains(l.InquiryId))
            .OrderByDescending(l => l.UpdatedAt)
            .Select(l => new LeadRow(
                l.Id, l.InquiryId, l.PropertyId, l.TenantId,
                l.DealValue, l.Notes, l.Status,
                l.CreatedAt, l.UpdatedAt))
            .ToListAsync(ct);

        var result = new List<BrokerLeadPipelineDto>();

        foreach (var brokerId in query.BrokerUserIds)
        {
            var brokerInquiryIds = assignments
                .Where(a => a.AssignedToId == brokerId)
                .Select(a => a.InquiryId)
                .ToHashSet();

            var brokerLeads = leads
                .Where(l => brokerInquiryIds.Contains(l.InquiryId))
                .ToList();

            static LeadCardDto ToCard(LeadRow l) => new(
                l.Id, l.InquiryId, l.PropertyId, l.TenantId,
                l.DealValue, l.Notes, l.CreatedAt, l.UpdatedAt);

            var kanban = new PipelineKanbanDto(
                New:         brokerLeads.Where(l => l.Status == LeadStatus.New).Select(ToCard).ToList(),
                Contacted:   brokerLeads.Where(l => l.Status == LeadStatus.Contacted).Select(ToCard).ToList(),
                Negotiating: brokerLeads.Where(l => l.Status == LeadStatus.Negotiating).Select(ToCard).ToList(),
                ClosedWon:   brokerLeads.Where(l => l.Status == LeadStatus.ClosedWon).Select(ToCard).ToList(),
                ClosedLost:  brokerLeads.Where(l => l.Status == LeadStatus.ClosedLost).Select(ToCard).ToList()
            );

            result.Add(new BrokerLeadPipelineDto(
                BrokerUserId: brokerId,
                Kanban: kanban,
                TotalLeads: brokerLeads.Count,
                ClosedWon: brokerLeads.Count(l => l.Status == LeadStatus.ClosedWon)
            ));
        }

        return result;
    }

    private sealed record LeadRow(
        Guid Id, Guid InquiryId, Guid PropertyId, Guid TenantId,
        decimal? DealValue, string? Notes, LeadStatus Status,
        DateTime CreatedAt, DateTime UpdatedAt);
}
