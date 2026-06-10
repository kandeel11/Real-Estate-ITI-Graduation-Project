using MaintenanceCompanies.Application.Interfaces;
using MaintenanceCompanies.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceCompanies.Infrastructure.Persistence;

public class MaintenanceModuleDbContext : DbContext, IMaintenanceModuleDbContext
{
    public MaintenanceModuleDbContext(DbContextOptions<MaintenanceModuleDbContext> options) : base(options) { }

    public DbSet<MaintenanceCompany> MaintenanceCompanies => Set<MaintenanceCompany>();
    public DbSet<MaintenanceCategory> MaintenanceCategories => Set<MaintenanceCategory>();
    public DbSet<CompanyCategory> CompanyCategories => Set<CompanyCategory>();
    public DbSet<CompanyWorkingHour> CompanyWorkingHours => Set<CompanyWorkingHour>();
    public DbSet<CompanyReview> CompanyReviews => Set<CompanyReview>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema("maintenance"); // module-scoped schema

        b.Entity<MaintenanceCategory>(e =>
        {
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
        });

        b.Entity<MaintenanceCompany>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.UserId).IsUnique();
            e.Property(x => x.CompanyName).HasMaxLength(200).IsRequired();
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(30).IsRequired();
            e.Property(x => x.City).HasMaxLength(100).IsRequired();
        });

        b.Entity<CompanyCategory>(e =>
        {
            e.HasKey(x => new { x.CompanyId, x.CategoryId });
            e.HasOne(x => x.Company).WithMany(c => c.Categories)
                .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Category).WithMany(c => c.Companies)
                .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<CompanyWorkingHour>(e =>
        {
            e.HasOne(x => x.Company).WithMany(c => c.WorkingHours)
                .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CompanyId, x.Day }).IsUnique();
        });

        b.Entity<CompanyReview>(e =>
        {
            e.HasOne(x => x.Company).WithMany(c => c.Reviews)
                .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.TicketId).IsUnique();         // one review per ticket
            e.Property(x => x.Rating).IsRequired();
        });
    }
}