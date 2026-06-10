using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Property.Domain.Enums;

namespace Property.Infrastructure.Persistence.Configurations;

public class PropertyConfiguration : IEntityTypeConfiguration<Domain.Entities.Property>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Property> builder)
    {
        builder.ToTable("properties");
        builder.HasKey(x => x.Id);

        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.Property(x => x.OwnerId).IsRequired();
        builder.Property(x => x.CompanyId);
        builder.Property(x => x.ListingBrokerId);

        builder.Property(x => x.Title).IsRequired().HasMaxLength(500);
        builder.Property(x => x.TitleAr).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Description).IsRequired();
        builder.Property(x => x.DescriptionAr).IsRequired();

        builder.Property(x => x.PropertyType)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(x => x.Price).IsRequired().HasPrecision(12, 2);
        builder.Property(x => x.Bedrooms).IsRequired();
        builder.Property(x => x.Bathrooms).IsRequired();
        builder.Property(x => x.AreaSqm).IsRequired().HasPrecision(10, 2);
        builder.Property(x => x.FloorNumber).IsRequired();
        builder.Property(x => x.TotalFloors).IsRequired();
        builder.Property(x => x.YearBuilt).IsRequired();
        builder.Property(x => x.IsFurnished).IsRequired();
        builder.Property(x => x.PetsAllowed).IsRequired();

        builder.Property(x => x.Address).IsRequired().HasMaxLength(500);
        builder.Property(x => x.ApartmentNumber);
        builder.Property(x => x.City).IsRequired().HasMaxLength(200);
        builder.Property(x => x.District).IsRequired().HasMaxLength(200);

        builder.Property(x => x.ListingSource)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasDefaultValue(PropertyStatus.Draft);

        builder.Property(x => x.IsFeatured).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.FeaturedUntil);

        builder.Property(x => x.AiDuplicateConfidence).HasPrecision(5, 4);

        builder.Property(x => x.VerificationStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasDefaultValue(VerificationStatus.Pending);

        builder.Property(x => x.RejectionReason);

        builder.Property(x => x.OwnershipGate)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(GateStatus.Pending);

        builder.Property(x => x.DuplicateGate)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(GateStatus.Pending);

        builder.Property(x => x.ViewsCount).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.InquiriesCount).IsRequired().HasDefaultValue(0);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt);
        builder.Property(x => x.DeletedAt);

        builder.HasIndex(x => x.OwnerId);
        builder.HasIndex(x => x.City);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.IsFeatured);

        builder.HasMany(x => x.Media)
               .WithOne() 
               .HasForeignKey("PropertyId")
               .OnDelete(DeleteBehavior.NoAction); 
    }
}
