using Domain.Entities;
using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence;

public class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Broker> Brokers => Set<Broker>();
    public DbSet<MaintenanceCompany> MaintenanceCompanies => Set<MaintenanceCompany>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.HasDefaultSchema("identity");
        mb.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.MarkAsUpdated();
        }
        return await base.SaveChangesAsync(ct);
    }
}
