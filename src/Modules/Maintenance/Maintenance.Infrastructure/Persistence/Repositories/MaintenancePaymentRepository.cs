using Maintenance.Application.Interfaces;
using Maintenance.Domain.Entities;
using Maintenance.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Maintenance.Infrastructure.Persistence.Repositories;

public class MaintenancePaymentRepository(MaintenanceDbContext db) : IMaintenancePaymentRepository
{
    public Task<MaintenancePayment?> GetByTicketIdAsync(Guid ticketId, CancellationToken ct = default)
        => db.Payments.FirstOrDefaultAsync(p => p.TicketId == ticketId, ct);

    public Task<MaintenancePayment?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Payments.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task AddAsync(MaintenancePayment payment, CancellationToken ct = default)
        => await db.Payments.AddAsync(payment, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}