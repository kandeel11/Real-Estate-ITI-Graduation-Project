using AI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI.Infrastructure.Persistence.Configurations
{
    public class AiUsageLogConfiguration : IEntityTypeConfiguration<AiUsageLog>
    {
        public void Configure(EntityTypeBuilder<AiUsageLog> builder)
        {
            builder.ToTable("ai_usage_logs", "ai");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.Property(x => x.UserId).IsRequired();
            builder.Property(x => x.CompanyId).IsRequired(false);
            builder.Property(x => x.SubscriptionId).IsRequired();

            builder.Property(x => x.FeatureType)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.RequestPayload).IsRequired(); // JSON
            builder.Property(x => x.ResponseSummary).IsRequired();

            builder.HasQueryFilter(x => x.DeletedAt == null);

        }
    }
}
