using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public class BrokerConfiguration : IEntityTypeConfiguration<Broker>
{
    public void Configure(EntityTypeBuilder<Broker> builder)
    {
        builder.ToTable("brokers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.CompanyId);
        builder.Property(x => x.LicenseNumber).IsRequired().HasMaxLength(100);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.AiPerformanceScore).HasPrecision(5, 2).HasDefaultValue(0m);
        builder.Property(x => x.ClosedDealsCount).HasDefaultValue(0);
        builder.Property(x => x.AvgResponseTimeMinutes).HasDefaultValue(0);
        builder.Property(x => x.ConversionRate).HasPrecision(5, 2).HasDefaultValue(0m);
        builder.Property(x => x.RatingAvg).HasPrecision(3, 2).HasDefaultValue(0m);
        builder.Property(x => x.TotalRatings).HasDefaultValue(0);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt);

        builder.HasIndex(x => x.LicenseNumber).IsUnique();
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.CompanyId);
    }
}
