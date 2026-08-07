using AI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI.Infrastructure.Persistence.Configurations
{
    public class DuplicateDetectionLogConfiguration : IEntityTypeConfiguration<DuplicateDetectionLog>
    {
        public void Configure(EntityTypeBuilder<DuplicateDetectionLog> builder)
        {
            builder.ToTable("duplicate_detection_logs", "ai");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

            builder.Property(x => x.NewPropertyId).IsRequired();
            builder.Property(x => x.SimilarPropertyId).IsRequired();
            builder.Property(x => x.AdminReviewerId).IsRequired(false);

            builder.Property(x => x.ImageSimilarityScore).HasPrecision(5, 2);
            builder.Property(x => x.LocationSimilarityScore).HasPrecision(5, 2);
            builder.Property(x => x.DescriptionSimilarityScore).HasPrecision(5, 2);
            builder.Property(x => x.LayoutSimilarityScore).HasPrecision(5, 2);
            builder.Property(x => x.OverallConfidence).HasPrecision(5, 2);

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.HasQueryFilter(x => x.DeletedAt == null);

        }
    }
}
