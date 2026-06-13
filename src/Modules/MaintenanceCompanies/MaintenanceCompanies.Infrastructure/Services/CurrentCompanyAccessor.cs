using Identity.Application.Interfaces;
using MaintenanceCompanies.Application.Interfaces;
using MaintenanceCompanies.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceCompanies.Infrastructure.Services;

public class CurrentCompanyAccessor : ICurrentCompanyAccessor
{
    private readonly IMaintenanceModuleDbContext _db;
    private readonly ICurrentUserService _currentUser; // الخدمة اللي بتجيب الـ ID من التوكن

    public CurrentCompanyAccessor(IMaintenanceModuleDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<MaintenanceCompany?> GetCurrentCompanyAsync(CancellationToken ct = default)
    {
        var userId = _currentUser.UserId?.ToString();
        if (string.IsNullOrEmpty(userId))
            return null;

        // بنجيب الشركة بالأقسام بتاعتها عشان بنحتاجها دايماً في الـ Business Logic
        return await _db.MaintenanceCompanies
            .Include(c => c.Categories)
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);
    }

    public async Task<MaintenanceCompany> RequireCurrentCompanyAsync(CancellationToken ct = default)
    {
        var company = await GetCurrentCompanyAsync(ct);

        if (company == null)
            throw new UnauthorizedAccessException("لم يتم العثور على ملف الشركة الخاص بك.");

        return company;
    }
}