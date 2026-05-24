using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Property.Domain.Entities;

namespace Property.Infrastructure.Persistence.Configurations;

public class PropertyAnalyticsConfiguration : IEntityTypeConfiguration<PropertyAnalytics>
{
    public void Configure(EntityTypeBuilder<PropertyAnalytics> builder)
    {
        builder.ToTable("property_analytics");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ViewedAt).IsRequired();
        builder.Property(x => x.ViewerId);
        builder.Property(x => x.IpAddress).HasMaxLength(45);
        builder.Property(x => x.Source).HasConversion<string>();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne<Domain.Entities.Property>()
            .WithMany()
            .HasForeignKey(x => x.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.PropertyId);
        builder.HasIndex(x => x.ViewedAt);
    }
}
