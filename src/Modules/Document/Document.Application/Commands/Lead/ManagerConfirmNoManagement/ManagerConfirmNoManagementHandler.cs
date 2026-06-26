using Document.Application.Interfaces;
using Document.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;

namespace Document.Application.Commands.Lead.ManagerConfirmNoManagement;

public sealed class ManagerConfirmNoManagementHandler
    : IRequestHandler<ManagerConfirmNoManagementCommand>
{
    private readonly IDocumentDbContext _db;
    private readonly IPublisher _publisher;
    private readonly INotificationService _notifications;

    public ManagerConfirmNoManagementHandler(
        IDocumentDbContext db,
        IPublisher publisher,
        INotificationService notifications)
    {
        _db = db;
        _publisher = publisher;
        _notifications = notifications;
    }

    public async Task Handle(ManagerConfirmNoManagementCommand cmd, CancellationToken ct)
    {
        var lead = await _db.Leads
            .FirstOrDefaultAsync(l => l.Id == cmd.LeadId, ct)
            ?? throw new KeyNotFoundException($"Lead {cmd.LeadId} not found.");

        if (lead.NoManagementPendingAt is null)
            throw new InvalidOperationException("No pending no-management request found for this lead.");

        if (lead.NoManagementManagerId != cmd.ManagerUserId)
            throw new UnauthorizedAccessException("Only the assigned manager can confirm this request.");

        // Finalize: mark property as Rented (no payment schedule)
        await _publisher.Publish(new PropertyRentedDirectlyEvent(
            lead.PropertyId, lead.OwnerId), ct);

        // Clear pending state
        lead.NoManagementPendingAt = null;
        lead.NoManagementManagerId = null;
        lead.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        // Notify tenant
        await _notifications.CreateInAppAsync(
            userId: lead.TenantId,
            title: "Deal Confirmed",
            body: "Your deal has been confirmed. The property is now rented. The company will not be providing management services for this property.",
            type: NotificationType.ContractSigned,
            relatedEntityType: "lead",
            relatedEntityId: lead.Id,
            ct: ct);
    }
}
