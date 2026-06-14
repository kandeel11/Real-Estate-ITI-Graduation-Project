using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public class MaintenanceCompanyConfiguration : IEntityTypeConfiguration<MaintenanceCompany>
{
    public void Configure(EntityTypeBuilder<MaintenanceCompany> builder)
    {
        builder.ToTable("maintenance_companies");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.CompanyName).IsRequired().HasMaxLength(255);
        builder.Property(x => x.ServiceTypes).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(255);
        builder.Property(x => x.Phone).IsRequired().HasMaxLength(50);
        builder.Property(x => x.City).IsRequired().HasMaxLength(100);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.RatingAvg).HasPrecision(3, 2).HasDefaultValue(0m);
        builder.Property(x => x.TotalRatings).HasDefaultValue(0);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt);

        builder.HasIndex(x => x.Email).IsUnique();
    }
}
