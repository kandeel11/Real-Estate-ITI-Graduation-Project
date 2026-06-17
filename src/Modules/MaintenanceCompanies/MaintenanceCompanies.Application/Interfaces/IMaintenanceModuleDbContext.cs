using MaintenanceCompanies.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceCompanies.Application.Interfaces
{
    public interface IMaintenanceModuleDbContext
    {
        DbSet<MaintenanceCompany> MaintenanceCompanies { get; }
        DbSet<MaintenanceCategory> MaintenanceCategories { get; }
        DbSet<CompanyCategory> CompanyCategories { get; }
        DbSet<CompanyWorkingHour> CompanyWorkingHours { get; }
        DbSet<CompanyReview> CompanyReviews { get; }

        // يفضل أيضاً إضافة دالة الحفظ لو هتحتاج تستخدمها من خلال الـ Interface
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
