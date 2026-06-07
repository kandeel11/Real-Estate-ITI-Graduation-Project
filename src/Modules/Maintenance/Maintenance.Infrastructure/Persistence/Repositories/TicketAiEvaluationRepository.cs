using Maintenance.Application.Interfaces;
using Maintenance.Domain.Entities;
using Maintenance.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Maintenance.Infrastructure.Persistence.Repositories;

public class TicketAiEvaluationRepository(MaintenanceDbContext db) : ITicketAiEvaluationRepository
{
    public Task<TicketAiEvaluation?> GetByTicketIdAsync(Guid ticketId, CancellationToken ct = default)
        => db.AiEvaluations.FirstOrDefaultAsync(e => e.TicketId == ticketId, ct);

    public async Task AddAsync(TicketAiEvaluation evaluation, CancellationToken ct = default)
        => await db.AiEvaluations.AddAsync(evaluation, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}