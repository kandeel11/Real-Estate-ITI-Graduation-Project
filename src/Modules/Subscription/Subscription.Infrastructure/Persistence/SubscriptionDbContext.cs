using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Subscription.Domain.Entities;

namespace Subscription.Infrastructure.Persistence;

public class SubscriptionDbContext : DbContext
{
    public SubscriptionDbContext(DbContextOptions<SubscriptionDbContext> options) : base(options) { }

    public DbSet<SubscriptionPlan> Plans => Set<SubscriptionPlan>();
    public DbSet<PlanFeature> PlanFeatures => Set<PlanFeature>();
    public DbSet<Domain.Entities.Subscription> Subscriptions => Set<Domain.Entities.Subscription>();
    public DbSet<SubscriptionPayment> Payments => Set<SubscriptionPayment>();
    public DbSet<SubscriptionUsage> Usage => Set<SubscriptionUsage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("subscription");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SubscriptionDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.MarkAsUpdated();
        }
        return await base.SaveChangesAsync(cancellationToken);
    }
}
