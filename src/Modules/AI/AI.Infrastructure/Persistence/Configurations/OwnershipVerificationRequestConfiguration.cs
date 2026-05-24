using AI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AI.Infrastructure.Persistence.Configurations
{
    public class OwnershipVerificationRequestConfiguration : IEntityTypeConfiguration<OwnershipVerificationRequest>
    {
        public void Configure(EntityTypeBuilder<OwnershipVerificationRequest> builder)
        {
            builder.ToTable("ownership_verification_requests", "ai");

            builder.HasKey(r => r.Id);
            builder.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

            builder.Property(x => x.AiConfidenceScore).HasPrecision(5, 2);
            builder.Property(x => x.AiNameMatchScore).HasPrecision(5, 2);
            builder.Property(x => x.AiAuthenticityScore).HasPrecision(5, 2);

            builder.Property(x => x.PropertyId).IsRequired();
            builder.Property(x => x.OwnerId).IsRequired();
            builder.Property(x => x.AdminReviewerId).IsRequired(false);

            builder.HasQueryFilter(x => x.DeletedAt == null);
        }
    }
}
