using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Property.Domain.Entities;

namespace Property.Infrastructure.Persistence.Configurations;

public class PropertyAmenityConfiguration : IEntityTypeConfiguration<PropertyAmenity>
{
    public void Configure(EntityTypeBuilder<PropertyAmenity> builder)
    {
        builder.ToTable("property_amenities");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne<Domain.Entities.Property>()
            .WithMany()
            .HasForeignKey(x => x.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Amenity>()
            .WithMany()
            .HasForeignKey(x => x.AmenityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.PropertyId, x.AmenityId }).IsUnique();
    }
}
