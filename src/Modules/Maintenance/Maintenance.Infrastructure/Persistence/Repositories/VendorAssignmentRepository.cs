using Maintenance.Application.Interfaces;
using Maintenance.Domain.Entities;
using Maintenance.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Maintenance.Infrastructure.Persistence.Repositories;

public class VendorAssignmentRepository(MaintenanceDbContext db) : IVendorAssignmentRepository
{
    public Task<TicketVendorAssignment?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.VendorAssignments.FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<TicketVendorAssignment?> GetCurrentByTicketIdAsync(Guid ticketId, CancellationToken ct = default)
        => db.VendorAssignments.Where(a => a.TicketId == ticketId)
            .OrderByDescending(a => a.CreatedAt).FirstOrDefaultAsync(ct);

    public Task<List<TicketVendorAssignment>> GetAllByTicketIdAsync(Guid ticketId, CancellationToken ct = default)
        => db.VendorAssignments.Where(a => a.TicketId == ticketId)
            .OrderByDescending(a => a.CreatedAt).ToListAsync(ct);

    public async Task AddAsync(TicketVendorAssignment assignment, CancellationToken ct = default)
        => await db.VendorAssignments.AddAsync(assignment, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}