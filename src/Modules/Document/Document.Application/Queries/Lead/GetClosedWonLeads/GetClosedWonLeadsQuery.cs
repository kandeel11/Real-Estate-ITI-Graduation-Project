using Document.Application.Interfaces;
using Document.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Queries.Lead.GetClosedWonLeads;

/// <summary>One closed-won lead, with its property and (if any) assigned broker.</summary>
public record ClosedWonLeadDto(Guid LeadId, Guid PropertyId, Guid? AssignedBrokerId);

/// <summary>
/// Returns every closed-won lead so the backfill can credit the managing broker.
/// The assigned broker is taken from InquiryAssignment when present; the caller
/// falls back to the property's listing broker otherwise.
/// </summary>
public record GetClosedWonLeadsQuery() : IRequest<List<ClosedWonLeadDto>>;

public class GetClosedWonLeadsHandler : IRequestHandler<GetClosedWonLeadsQuery, List<ClosedWonLeadDto>>
{
    private readonly IDocumentDbContext _db;

    public GetClosedWonLeadsHandler(IDocumentDbContext db) => _db = db;

    public async Task<List<ClosedWonLeadDto>> Handle(GetClosedWonLeadsQuery query, CancellationToken ct)
    {
        return await (
            from l in _db.Leads
            where l.Status == LeadStatus.ClosedWon
            join a in _db.InquiryAssignments on l.InquiryId equals a.InquiryId into ass
            from a in ass.DefaultIfEmpty()
            select new ClosedWonLeadDto(l.Id, l.PropertyId, a != null ? a.AssignedToId : (Guid?)null)
        ).ToListAsync(ct);
    }
}
