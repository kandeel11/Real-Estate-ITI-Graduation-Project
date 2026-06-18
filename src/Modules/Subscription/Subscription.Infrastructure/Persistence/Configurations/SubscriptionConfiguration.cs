using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subscription.Domain.Enums;

namespace Subscription.Infrastructure.Persistence.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Domain.Entities.Subscription>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Subscription> builder)
    {
        builder.ToTable("subscriptions");
        builder.HasKey(x => x.Id);

        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.Property(x => x.PlanId).IsRequired();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.CompanyId);
        builder.Property(x => x.BillingCycle).IsRequired().HasConversion<string>();
        builder.Property(x => x.Status).IsRequired().HasConversion<string>();
        builder.Property(x => x.StartDate).IsRequired();
        builder.Property(x => x.EndDate).IsRequired();
        builder.Property(x => x.TrialEndDate);
        builder.Property(x => x.IsAutoRenew).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CancelledAt);
        builder.Property(x => x.CancellationReason);
        builder.Property(x => x.UpgradeFromPlanId);
        builder.Property(x => x.DowngradeToPlanId);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt);
        builder.Property(x => x.DeletedAt);

        builder.HasOne(x => x.Plan)
            .WithMany()
            .HasForeignKey(x => x.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.UsageRecords)
            .WithOne(u => u.Subscription)
            .HasForeignKey(u => u.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.Status);
    }
}
