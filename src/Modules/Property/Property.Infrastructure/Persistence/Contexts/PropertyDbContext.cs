using Microsoft.EntityFrameworkCore;
using Property.Domain.Entities;

namespace Property.Infrastructure.Persistence.Contexts;

public class PropertyDbContext : DbContext
{
    public PropertyDbContext(DbContextOptions<PropertyDbContext> options) : base(options) { }

    public DbSet<Domain.Entities.Property> Properties => Set<Domain.Entities.Property>();
    public DbSet<PropertyMedia> PropertyMedia => Set<PropertyMedia>();
    public DbSet<Amenity> Amenities => Set<Amenity>();
    public DbSet<PropertyAmenity> PropertyAmenities => Set<PropertyAmenity>();
    public DbSet<PropertyAnalytics> PropertyAnalytics => Set<PropertyAnalytics>();
    public DbSet<SavedProperty> SavedProperties => Set<SavedProperty>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("property");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PropertyDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
