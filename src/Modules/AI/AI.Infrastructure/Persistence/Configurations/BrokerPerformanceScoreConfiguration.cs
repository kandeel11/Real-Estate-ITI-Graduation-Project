using AI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI.Infrastructure.Persistence.Configurations
{
    public class BrokerPerformanceScoreConfiguration : IEntityTypeConfiguration<BrokerPerformanceScore>
    {
        public void Configure(EntityTypeBuilder<BrokerPerformanceScore> builder)
        {
            builder.ToTable("broker_performance_scores", "ai");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

            builder.Property(x => x.BrokerId).IsRequired();

            builder.Property(x => x.AvgRating).HasPrecision(3, 2);
            builder.Property(x => x.ConversionRate).HasPrecision(5, 2);
            builder.Property(x => x.TenantSatisfactionScore).HasPrecision(5, 2);
            builder.Property(x => x.AiPerformanceScore).HasPrecision(5, 2);

            //  JSON
            builder.Property(x => x.AiScoreBreakdown).IsRequired();

            builder.HasQueryFilter(x => x.DeletedAt == null);

        }
    }
}
