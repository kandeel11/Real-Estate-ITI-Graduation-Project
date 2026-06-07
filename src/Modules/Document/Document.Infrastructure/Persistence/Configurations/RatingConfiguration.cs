using Document.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Document.Infrastructure.Persistence.Configurations;

public class RatingConfiguration : IEntityTypeConfiguration<Rating>
{
    public void Configure(EntityTypeBuilder<Rating> b)
    {
        b.ToTable("ratings");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.ReviewerId).IsRequired();
        b.Property(x => x.RatingValue).IsRequired();
        b.Property(x => x.ReviewText).HasColumnType("nvarchar(max)");
        b.Property(x => x.ModerationStatus).HasConversion<string>().HasMaxLength(50).IsRequired();
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.UpdatedAt).IsRequired();
        b.HasQueryFilter(x => x.DeletedAt == null);
        b.HasIndex(x => x.ModerationStatus);
    }
}