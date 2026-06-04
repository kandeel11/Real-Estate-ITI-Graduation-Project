using AI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI.Infrastructure.Persistence.Configurations
{
    public class AdminAnalyticsSnapshotConfiguration : IEntityTypeConfiguration<AdminAnalyticsSnapshot>
    {
        public void Configure(EntityTypeBuilder<AdminAnalyticsSnapshot> builder)
        {
            builder.ToTable("admin_analytics_snapshots", "ai");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            builder.Property(x => x.TotalRevenue).HasPrecision(18, 2);
            //  JSON
            builder.Property(x => x.MostActiveCities).IsRequired();
            builder.Property(x => x.MostPopularPlans).IsRequired();
            builder.Property(x => x.MostActiveCompanies).IsRequired();
            builder.Property(x => x.VerificationStats).IsRequired();
            builder.HasQueryFilter(x => x.DeletedAt == null);
        }
    }
}
