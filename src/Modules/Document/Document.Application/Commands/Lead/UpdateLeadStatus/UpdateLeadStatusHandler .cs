using Document.Application.Interfaces;
using Document.Domain.Enums;
using Document.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;

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
            ?? throw new KeyNotFoundException($"Lead {cmd.LeadId} not found");

        // Accept both lowercase snake-case ("closed_won") and PascalCase ("ClosedWon"),
        // case-insensitively. Invalid values throw ArgumentException → controller maps to 400.
        var status = ParseStatus(cmd.NewStatus);
        lead.Status = status;

        if (cmd.Notes != null)
            lead.Notes = cmd.Notes;

        if (status == LeadStatus.ClosedWon || status == LeadStatus.ClosedLost)
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

            // Credit the assigned broker's ClosedDealsCount (Identity module
            // listens via Shared.Kernel — modules never reference each other)
            var assignedBrokerUserId = await _db.InquiryAssignments
                .Where(a => a.InquiryId == lead.InquiryId)
                .Select(a => (Guid?)a.AssignedToId)
                .FirstOrDefaultAsync(ct);

            if (assignedBrokerUserId is not null)
            {
                await _publisher.Publish(new BrokerDealClosedEvent(
                    assignedBrokerUserId.Value, lead.Id, lead.DealValue), ct);
            }
        }
    }

    /// <summary>
    /// Maps an incoming status string to <see cref="LeadStatus"/>. Accepts both
    /// snake_case ("closed_won") and PascalCase ("ClosedWon"), case-insensitively.
    /// Throws <see cref="ArgumentException"/> for unknown values.
    /// </summary>
    private static LeadStatus ParseStatus(string? raw)
    {
        var normalized = (raw ?? string.Empty).Replace("_", string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "new" => LeadStatus.New,
            "contacted" => LeadStatus.Contacted,
            "negotiating" => LeadStatus.Negotiating,
            "closedwon" => LeadStatus.ClosedWon,
            "closedlost" => LeadStatus.ClosedLost,
            _ => throw new ArgumentException($"Invalid lead status: '{raw}'")
        };
    }
}