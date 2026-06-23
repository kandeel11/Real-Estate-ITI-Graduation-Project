using Document.Application.Interfaces;
using Document.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Queries.Lease.GetLeadLeaseStates;

/// <summary>
/// Per-lead lease state for the broker/owner pipeline's closed-won column:
///  • <see cref="HasSentLease"/> — a non-rejected lease was already sent to the tenant
///    (or is active) → show "Lease Created".
///  • <see cref="DraftLeaseId"/> — a non-rejected lease was uploaded but not yet sent
///    → offer "Resume" (jump back to the review step).
/// A declined (rejected) lease counts as neither, so the broker can recreate it.
/// </summary>
public record LeadLeaseState(bool HasSentLease, Guid? DraftLeaseId);

public record GetLeadLeaseStatesQuery(IReadOnlyCollection<Guid> LeadIds)
    : IRequest<Dictionary<Guid, LeadLeaseState>>;

public class GetLeadLeaseStatesHandler(IDocumentDbContext db)
    : IRequestHandler<GetLeadLeaseStatesQuery, Dictionary<Guid, LeadLeaseState>>
{
    public async Task<Dictionary<Guid, LeadLeaseState>> Handle(GetLeadLeaseStatesQuery q, CancellationToken ct)
    {
        if (q.LeadIds.Count == 0) return new Dictionary<Guid, LeadLeaseState>();

        var leases = await db.Leases
            .Where(l => l.LeadId != null
                        && q.LeadIds.Contains(l.LeadId.Value)
                        && l.DeletedAt == null
                        && l.ExtractionStatus != LeaseExtractionStatus.Rejected)
            .Select(l => new { LeadId = l.LeadId!.Value, l.Id, l.OwnerConfirmedAt, l.CreatedAt })
            .ToListAsync(ct);

        return leases
            .GroupBy(l => l.LeadId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    if (g.Any(x => x.OwnerConfirmedAt != null))
                        return new LeadLeaseState(true, null);

                    var draft = g.OrderByDescending(x => x.CreatedAt).First();
                    return new LeadLeaseState(false, draft.Id);
                });
    }
}
