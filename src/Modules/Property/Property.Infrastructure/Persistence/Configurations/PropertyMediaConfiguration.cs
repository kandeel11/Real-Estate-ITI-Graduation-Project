using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Property.Domain.Entities;

namespace Property.Infrastructure.Persistence.Configurations;

public class PropertyMediaConfiguration : IEntityTypeConfiguration<PropertyMedia>
{
    public void Configure(EntityTypeBuilder<PropertyMedia> builder)
    {
        builder.ToTable("property_media");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MediaUrl).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.MediaType).IsRequired().HasConversion<string>();
        builder.Property(x => x.IsPrimary).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.SortOrder).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.FileSizeKb).IsRequired();
        builder.Property(x => x.UploadedAt).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.PropertyId);
    }
}
