using Document.Application.Interfaces;
using Document.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;

namespace Document.Application.Commands.Lead.CloseNoManagement;

public sealed class CloseNoManagementHandler
    : IRequestHandler<CloseNoManagementCommand, CloseNoManagementResult>
{
    private readonly IDocumentDbContext _db;
    private readonly IPublisher _publisher;
    private readonly INotificationService _notifications;

    public CloseNoManagementHandler(
        IDocumentDbContext db,
        IPublisher publisher,
        INotificationService notifications)
    {
        _db = db;
        _publisher = publisher;
        _notifications = notifications;
    }

    public async Task<CloseNoManagementResult> Handle(
        CloseNoManagementCommand cmd, CancellationToken ct)
    {
        var lead = await _db.Leads
            .FirstOrDefaultAsync(l => l.Id == cmd.LeadId, ct)
            ?? throw new KeyNotFoundException($"Lead {cmd.LeadId} not found.");

        if (lead.Status != LeadStatus.ClosedWon)
            throw new InvalidOperationException("Lead must be in Closed Won status before closing without management.");

        // ── Independent broker: finalize immediately ─────────────────────────
        if (cmd.CompanyManagerId is null)
        {
            await _publisher.Publish(new PropertyRentedDirectlyEvent(
                lead.PropertyId, lead.OwnerId), ct);

            await _notifications.CreateInAppAsync(
                userId: lead.TenantId,
                title: "Deal Complete",
                body: "Your deal has been finalized. The property is now rented. No management service will be provided.",
                type: NotificationType.ContractSigned,
                relatedEntityType: "lead",
                relatedEntityId: lead.Id,
                ct: ct);

            return new CloseNoManagementResult("closed");
        }

        // ── Company broker: wait for manager confirmation ────────────────────
        lead.NoManagementPendingAt  = DateTime.UtcNow;
        lead.NoManagementManagerId  = cmd.CompanyManagerId;
        lead.UpdatedAt              = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _notifications.CreateInAppAsync(
            userId: cmd.CompanyManagerId.Value,
            title: "No-Management Deal — Confirmation Required",
            body: "A broker closed a deal without requesting company management. Please review and confirm to complete the transaction.",
            type: NotificationType.ContractSigned,
            relatedEntityType: "lead",
            relatedEntityId: lead.Id,
            ct: ct);

        return new CloseNoManagementResult("pending_manager_confirmation");
    }
}
