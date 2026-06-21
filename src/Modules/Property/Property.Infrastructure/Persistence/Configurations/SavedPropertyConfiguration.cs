using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Property.Domain.Entities;

namespace Property.Infrastructure.Persistence.Configurations;

public class SavedPropertyConfiguration : IEntityTypeConfiguration<SavedProperty>
{
    public void Configure(EntityTypeBuilder<SavedProperty> builder)
    {
        builder.ToTable("saved_properties");
        builder.HasKey(x => x.Id);

        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.PropertyId).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        // One wishlist entry per (user, property).
        builder.HasIndex(x => new { x.UserId, x.PropertyId }).IsUnique();
        builder.HasIndex(x => x.UserId);
    }
}
