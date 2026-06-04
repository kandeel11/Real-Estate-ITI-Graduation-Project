using AI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI.Infrastructure.Persistence.Configurations
{
    public class AiPriceEstimationConfiguration : IEntityTypeConfiguration<AiPriceEstimation>
    {
        public void Configure(EntityTypeBuilder<AiPriceEstimation> builder)
        {
            builder.ToTable("ai_price_estimations", "ai");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.Property(x => x.EstimatedPriceMin).HasPrecision(18, 2);
            builder.Property(x => x.EstimatedPriceMax).HasPrecision(18, 2);
            builder.Property(x => x.EstimatedPrice).HasPrecision(18, 2);
            builder.Property(x => x.ConfidenceScore).HasPrecision(5, 2);


            builder.Property(x => x.ComparableProperties)
                .IsRequired();

        }
    }
}
