using Maintenance.Application.Interfaces;
using Maintenance.Domain.Entities;
using Maintenance.Domain.Enums;
using Maintenance.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Maintenance.Infrastructure.Persistence.Repositories;

public class MaintenanceTicketRepository(MaintenanceDbContext db) : IMaintenanceTicketRepository
{
    public Task<MaintenanceTicket?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.MaintenanceTickets.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<MaintenanceTicket?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
        => db.MaintenanceTickets
            .Include(t => t.Attachments)
            .Include(t => t.Comments)
            .Include(t => t.AiEvaluation)
            .Include(t => t.CurrentVendorAssignment)
            .Include(t => t.StatusHistory)
            .Include(t => t.Payment)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<List<MaintenanceTicket>> GetByTenantIdAsync(Guid tenantId, TicketStatus? status, CancellationToken ct = default)
    {
        var q = db.MaintenanceTickets.Include(t => t.Attachments).Where(t => t.TenantId == tenantId);
        if (status.HasValue) q = q.Where(t => t.Status == status.Value);
        return q.OrderByDescending(t => t.CreatedAt).ToListAsync(ct);
    }

    public Task<List<MaintenanceTicket>> GetByOwnerIdAsync(Guid ownerId, TicketStatus? status, CancellationToken ct = default)
    {
        var q = db.MaintenanceTickets.Include(t => t.Attachments).Where(t => t.OwnerId == ownerId);
        if (status.HasValue) q = q.Where(t => t.Status == status.Value);
        return q.OrderByDescending(t => t.CreatedAt).ToListAsync(ct);
    }

    public Task<List<MaintenanceTicket>> GetByPropertyIdAsync(Guid propertyId, CancellationToken ct = default)
        => db.MaintenanceTickets.Where(t => t.PropertyId == propertyId)
            .OrderByDescending(t => t.CreatedAt).ToListAsync(ct);

    public Task<List<MaintenanceTicket>> GetPendingAiEvaluationAsync(CancellationToken ct = default)
        => db.MaintenanceTickets.Where(t => t.Status == TicketStatus.Submitted).ToListAsync(ct);

    public Task<int> CountAsync(CancellationToken ct = default)
        => db.MaintenanceTickets.IgnoreQueryFilters().CountAsync(ct);

    public async Task AddAsync(MaintenanceTicket ticket, CancellationToken ct = default)
        => await db.MaintenanceTickets.AddAsync(ticket, ct);

    public async Task AddAttachmentAsync(MaintenanceTicketAttachment attachment, CancellationToken ct = default)
        => await db.Attachments.AddAsync(attachment, ct);

    public Task<MaintenanceTicketAttachment?> GetAttachmentByIdAsync(Guid attachmentId, CancellationToken ct = default)
    => db.Attachments.FirstOrDefaultAsync(a => a.Id == attachmentId, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}