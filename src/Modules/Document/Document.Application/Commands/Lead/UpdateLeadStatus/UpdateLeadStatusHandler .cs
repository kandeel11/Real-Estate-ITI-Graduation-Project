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
    private readonly INotificationService _notifications;

    public UpdateLeadStatusHandler(
        IDocumentDbContext db,
        IPublisher publisher,
        INotificationService notifications)
    {
        _db = db;
        _publisher = publisher;
        _notifications = notifications;
    }

    public async Task Handle(UpdateLeadStatusCommand cmd, CancellationToken ct)
    {
        var lead = await _db.Leads.FindAsync(new object[] { cmd.LeadId }, ct)
            ?? throw new KeyNotFoundException($"Lead {cmd.LeadId} not found");

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

        // Notify the tenant that their lead has progressed in the pipeline.
        var (title, body) = status switch
        {
            LeadStatus.Contacted   => ("Lead Update", "A broker has contacted you about your inquiry."),
            LeadStatus.Negotiating => ("Lead Update", "Your inquiry is now in the negotiation stage."),
            LeadStatus.ClosedWon   => ("Congratulations!", "Your inquiry has been closed successfully. A deal has been made!"),
            LeadStatus.ClosedLost  => ("Inquiry Closed", "Unfortunately, your inquiry has been closed without a deal."),
            _                      => ("Lead Update", $"Your inquiry status has been updated to: {status}.")
        };

        await _notifications.CreateInAppAsync(
            userId: lead.TenantId,
            title: title,
            body: body,
            type: NotificationType.LeadStatusChanged,
            relatedEntityType: "lead",
            relatedEntityId: lead.Id,
            ct: ct);

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

    private static LeadStatus ParseStatus(string? raw)
    {
        var normalized = (raw ?? string.Empty).Replace("_", string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "new"        => LeadStatus.New,
            "contacted"  => LeadStatus.Contacted,
            "negotiating"=> LeadStatus.Negotiating,
            "closedwon"  => LeadStatus.ClosedWon,
            "closedlost" => LeadStatus.ClosedLost,
            _ => throw new ArgumentException($"Invalid lead status: '{raw}'")
        };
    }
}
