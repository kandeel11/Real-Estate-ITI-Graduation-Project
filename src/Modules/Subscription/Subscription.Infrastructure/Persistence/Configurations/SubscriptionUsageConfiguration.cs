using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subscription.Domain.Entities;

namespace Subscription.Infrastructure.Persistence.Configurations;

public class SubscriptionUsageConfiguration : IEntityTypeConfiguration<SubscriptionUsage>
{
    public void Configure(EntityTypeBuilder<SubscriptionUsage> builder)
    {
        builder.ToTable("subscription_usage");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SubscriptionId).IsRequired();
        builder.Property(x => x.PeriodStart).IsRequired();
        builder.Property(x => x.PeriodEnd).IsRequired();
        builder.Property(x => x.PropertiesUsed).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.BrokersUsed).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.AiRequestsUsed).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.StorageUsedGb).IsRequired().HasPrecision(10, 4).HasDefaultValue(0m);
        builder.Property(x => x.SavedListingsUsed).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.SavedSearchesUsed).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.InquiriesUsed).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt);
        builder.Property(x => x.DeletedAt);

        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.HasIndex(x => x.SubscriptionId);
    }
}
