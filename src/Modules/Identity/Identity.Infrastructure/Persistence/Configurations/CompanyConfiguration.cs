using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("companies");
        builder.HasKey(x => x.Id);

        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.Property(x => x.ManagerId).IsRequired();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(255);
        builder.Property(x => x.CommercialRegister).IsRequired().HasMaxLength(100);
        builder.Property(x => x.LogoUrl).HasMaxLength(1000);
        builder.Property(x => x.Website).HasMaxLength(500);
        builder.Property(x => x.Description);
        builder.Property(x => x.City).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Address).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Phone).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(255);
        builder.Property(x => x.IsVerified).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.RatingAvg).HasPrecision(3, 2).HasDefaultValue(0m);
        builder.Property(x => x.TotalRatings).HasDefaultValue(0);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt);
        builder.Property(x => x.DeletedAt);

        builder.HasIndex(x => x.CommercialRegister).IsUnique();
    }
}
