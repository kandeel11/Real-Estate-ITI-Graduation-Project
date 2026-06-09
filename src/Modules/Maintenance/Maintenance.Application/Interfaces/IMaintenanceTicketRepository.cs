using Maintenance.Domain.Entities;
using Maintenance.Domain.Enums;

namespace Maintenance.Application.Interfaces;

public interface IMaintenanceTicketRepository
{
    Task<MaintenanceTicket?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<MaintenanceTicket?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<List<MaintenanceTicket>> GetByTenantIdAsync(Guid tenantId, TicketStatus? status = null, CancellationToken ct = default);
    Task<List<MaintenanceTicket>> GetByOwnerIdAsync(Guid ownerId, TicketStatus? status = null, CancellationToken ct = default);
    Task<List<MaintenanceTicket>> GetByPropertyIdAsync(Guid propertyId, CancellationToken ct = default);
    Task<List<MaintenanceTicket>> GetByVendorIdAsync(Guid vendorId, CancellationToken ct = default);  // ← new
    Task<List<MaintenanceTicket>> GetPendingAiEvaluationAsync(CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    Task<MaintenanceTicketAttachment?> GetAttachmentByIdAsync(Guid attachmentId, CancellationToken ct = default);
    Task AddAttachmentAsync(MaintenanceTicketAttachment attachment, CancellationToken ct = default);
    Task AddAsync(MaintenanceTicket ticket, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}