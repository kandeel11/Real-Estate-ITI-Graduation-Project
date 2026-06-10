using MaintenanceCompanies.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceCompanies.Application.Services;

public class CompanyAssignmentService : ICompanyAssignmentService
{
    private readonly IMaintenanceModuleDbContext _db;

    public CompanyAssignmentService(IMaintenanceModuleDbContext db) => _db = db;

    public async Task<Guid?> FindBestCompanyAsync(Guid categoryId, string city)
    {
        // Strategy: active + same city + has category, then best rating, then least loaded
        var candidate = await _db.MaintenanceCompanies
            .Where(c => c.IsActive)
            .Where(c => c.City == city)
            .Where(c => c.Categories.Any(cc => cc.CategoryId == categoryId))
            .OrderByDescending(c => c.AverageRating)
            .ThenBy(c => c.ActiveTicketsCount)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync();

        // Fallback: ignore city if no match
        if (candidate == null)
        {
            candidate = await _db.MaintenanceCompanies
                .Where(c => c.IsActive)
                .Where(c => c.Categories.Any(cc => cc.CategoryId == categoryId))
                .OrderByDescending(c => c.AverageRating)
                .ThenBy(c => c.ActiveTicketsCount)
                .Select(c => (Guid?)c.Id)
                .FirstOrDefaultAsync();
        }

        return candidate;
    }

    public async Task IncrementActiveTicketsAsync(Guid companyId)
    {
        var c = await _db.MaintenanceCompanies.FindAsync(companyId);
        if (c == null) return;

        c.IncrementActiveTickets(); // Using Domain Method
        await _db.SaveChangesAsync();
    }

    public async Task OnTicketCompletedAsync(Guid companyId)
    {
        var c = await _db.MaintenanceCompanies.FindAsync(companyId);
        if (c == null) return;

        c.CompleteTicket(); // Using Domain Method
        await _db.SaveChangesAsync();
    }
}