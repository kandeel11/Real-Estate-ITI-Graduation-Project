using Document.Application.Interfaces;
using Document.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Queries.Inquiry;

/// <summary>
/// Whether the tenant is allowed to send a new inquiry, and if not, why.
/// A tenant may have only one engagement in flight at a time (see the rules below).
/// </summary>
public record TenantInquiryGate(bool Allowed, string ReasonCode, string Message);

public record GetTenantInquiryGateQuery(Guid TenantId) : IRequest<TenantInquiryGate>;

public class GetTenantInquiryGateHandler(IDocumentDbContext db, ISender mediator)
    : IRequestHandler<GetTenantInquiryGateQuery, TenantInquiryGate>
{
    public async Task<TenantInquiryGate> Handle(GetTenantInquiryGateQuery q, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        // 1) Currently renting — block while a lease is ongoing (active OR pending the
        //    tenant's acceptance) and hasn't ended, been terminated, or been declined.
        var hasOngoingLease = await db.Leases.AnyAsync(l =>
            l.TenantId == q.TenantId &&
            l.DeletedAt == null &&
            l.TerminatedAt == null &&
            l.ExtractionStatus != LeaseExtractionStatus.Rejected &&
            (l.EndDate == null || l.EndDate >= today), ct);

        // A won lead means the tenant secured this property — keep them blocked until the
        // deal actually concludes as a finished tenancy (its lease ended or was terminated).
        // This covers the "lease not created yet" and "lease declined → being recreated"
        // windows too; only an ended/terminated lease releases the block.
        var hasWonLeadInProgress = await db.Leads.AnyAsync(le =>
            le.TenantId == q.TenantId &&
            le.Status == LeadStatus.ClosedWon &&
            !db.Leases.Any(l =>
                l.LeadId == le.Id && l.DeletedAt == null &&
                (l.TerminatedAt != null || (l.EndDate != null && l.EndDate < today))), ct);

        if (hasOngoingLease || hasWonLeadInProgress)
            return new TenantInquiryGate(false, "currently_renting",
                "You already have a lease in progress — you can only rent one property at a time.");

        // 2) Active lead — a deal already being worked by an agent.
        var hasActiveLead = await db.Leads.AnyAsync(le =>
            le.TenantId == q.TenantId &&
            (le.Status == LeadStatus.New ||
             le.Status == LeadStatus.Contacted ||
             le.Status == LeadStatus.Negotiating), ct);

        if (hasActiveLead)
            return new TenantInquiryGate(false, "active_lead",
                "You already have an inquiry being handled by an agent. Wait for it to close before sending another.");

        // 3) Unanswered inquiry — a pending inquiry with no broker/owner reply yet.
        var pendingIds = await db.Inquiries
            .Where(i => i.TenantId == q.TenantId && i.Status == InquiryStatus.Pending)
            .Select(i => i.Id)
            .ToListAsync(ct);

        if (pendingIds.Count > 0)
        {
            var answered = await mediator.Send(new GetAnsweredInquiriesQuery(pendingIds), ct);
            if (pendingIds.Any(id => !answered.Contains(id)))
                return new TenantInquiryGate(false, "unanswered_inquiry",
                    "You have an inquiry still awaiting a reply. Please wait for a response before sending another.");
        }

        return new TenantInquiryGate(true, "", "");
    }
}
