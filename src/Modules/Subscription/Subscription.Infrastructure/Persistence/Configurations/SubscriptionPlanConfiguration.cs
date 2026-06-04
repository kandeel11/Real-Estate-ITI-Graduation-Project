using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subscription.Domain.Entities;
using Subscription.Domain.Enums;

namespace Subscription.Infrastructure.Persistence.Configurations;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("subscription_plans");
        builder.HasKey(x => x.Id);

        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Tier).IsRequired().HasConversion<string>();
        builder.Property(x => x.PriceMonthly).IsRequired().HasPrecision(10, 2);
        builder.Property(x => x.PriceAnnual).IsRequired().HasPrecision(10, 2);
        builder.Property(x => x.MaxProperties).IsRequired();
        builder.Property(x => x.MaxBrokers).IsRequired();
        builder.Property(x => x.MaxAiRequestsPerMonth).IsRequired();
        builder.Property(x => x.MaxStorageGb).IsRequired();
        builder.Property(x => x.HasAdvancedAnalytics).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.HasFeaturedListings).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.HasAiPriceEstimation).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.HasAiChatbot).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.HasAiLeadPrioritization).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.HasAiDescriptionGenerator).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt);
        builder.Property(x => x.DeletedAt);

        builder.HasMany(x => x.Features)
            .WithOne(f => f.Plan)
            .HasForeignKey(f => f.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.Tier);
        builder.HasIndex(x => x.IsActive);
    }
}
