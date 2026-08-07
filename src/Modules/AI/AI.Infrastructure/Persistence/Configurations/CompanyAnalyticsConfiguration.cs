using AI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI.Infrastructure.Persistence.Configurations
{
    public class CompanyAnalyticsConfiguration : IEntityTypeConfiguration<CompanyAnalytics>
    {
        public void Configure(EntityTypeBuilder<CompanyAnalytics> builder)
        {
            builder.ToTable("company_analytics", "ai");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

            builder.Property(x => x.CompanyId).IsRequired();

            builder.Property(x => x.ConversionRate).HasPrecision(5, 2);
            builder.Property(x => x.AvgTenantSatisfaction).HasPrecision(3, 2);

            //JSON
            builder.Property(x => x.MostViewedProperties).IsRequired();
            builder.Property(x => x.MostRentedAreas).IsRequired();

            builder.HasQueryFilter(x => x.DeletedAt == null);
        }

    }
}
