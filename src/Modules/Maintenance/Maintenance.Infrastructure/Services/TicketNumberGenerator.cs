using Maintenance.Application.Interfaces;
using Maintenance.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Maintenance.Infrastructure.Services;

public class TicketNumberGenerator(MaintenanceDbContext db) : ITicketNumberGenerator
{
    public async Task<string> GenerateAsync(CancellationToken ct = default)
    {
        var count = await db.MaintenanceTickets.IgnoreQueryFilters().CountAsync(ct) + 1;
        return $"TCK-{count:D4}";
    }
}