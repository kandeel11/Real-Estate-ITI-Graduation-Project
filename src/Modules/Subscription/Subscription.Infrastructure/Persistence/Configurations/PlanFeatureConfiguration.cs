using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subscription.Domain.Entities;

namespace Subscription.Infrastructure.Persistence.Configurations;

public class PlanFeatureConfiguration : IEntityTypeConfiguration<PlanFeature>
{
    public void Configure(EntityTypeBuilder<PlanFeature> builder)
    {
        builder.ToTable("plan_features");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PlanId).IsRequired();
        builder.Property(x => x.FeatureKey).IsRequired().HasMaxLength(100);
        builder.Property(x => x.FeatureLabel).IsRequired().HasMaxLength(255);
        builder.Property(x => x.FeatureValue).IsRequired().HasMaxLength(255);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt);
        builder.Property(x => x.DeletedAt);

        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.HasIndex(x => x.PlanId);
    }
}
