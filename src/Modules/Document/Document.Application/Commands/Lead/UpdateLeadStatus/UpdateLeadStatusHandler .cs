using Document.Application.Interfaces;
using Document.Domain.Enums;
using Document.Domain.Events;
using MediatR;

namespace Document.Application.Commands.Lead.UpdateLeadStatus;

public class UpdateLeadStatusHandler : IRequestHandler<UpdateLeadStatusCommand>
{
    private readonly IDocumentDbContext _db;
    private readonly IPublisher _publisher;

    public UpdateLeadStatusHandler(IDocumentDbContext db, IPublisher publisher)
    { _db = db; _publisher = publisher; }

    public async Task Handle(UpdateLeadStatusCommand cmd, CancellationToken ct)
    {
        var lead = await _db.Leads.FindAsync(new object[] { cmd.LeadId }, ct)
            ?? throw new Exception("Lead not found");

        // Validate الـ transition
        var validStatuses = new[] { "new", "contacted", "negotiating", "closed_won", "closed_lost" };
        if (!validStatuses.Contains(cmd.NewStatus))
            throw new Exception($"Invalid status: {cmd.NewStatus}");

        lead.Status = cmd.NewStatus switch
        {
            "new" => LeadStatus.New,
            "contacted" => LeadStatus.Contacted,
            "negotiating" => LeadStatus.Negotiating,
            "closed_won" => LeadStatus.ClosedWon,
            "closed_lost" => LeadStatus.ClosedLost,
            _ => throw new Exception("Invalid status")
        };

        if (cmd.Notes != null)
            lead.Notes = cmd.Notes;

        if (cmd.NewStatus == "closed_won" || cmd.NewStatus == "closed_lost")
        {
            lead.ClosedAt = DateTime.UtcNow;
            if (cmd.DealValue.HasValue)
                lead.DealValue = cmd.DealValue;
        }

        lead.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        // لو closed_won ابعت event
        if (lead.Status == LeadStatus.ClosedWon)
        {
            await _publisher.Publish(new LeadClosedWonEvent
            {
                LeadId = lead.Id,
                InquiryId = lead.InquiryId,
                PropertyId = lead.PropertyId,
                TenantId = lead.TenantId,
                OwnerId = lead.OwnerId,
                DealValue = lead.DealValue
            }, ct);
        }
    }
}