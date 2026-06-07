using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(x => x.Id);

        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.Property(x => x.FullName).IsRequired().HasMaxLength(255);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(255);
        builder.Property(x => x.PasswordHash).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Phone).IsRequired().HasMaxLength(50);
        builder.Property(x => x.NationalIdNumber).HasMaxLength(50);
        builder.Property(x => x.ProfileImageUrl).HasMaxLength(1000);
        builder.Property(x => x.Role).IsRequired().HasMaxLength(50);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.IsEmailVerified).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.EmailVerifiedAt);
        builder.Property(x => x.EmailVerificationToken).HasMaxLength(255);
        builder.Property(x => x.EmailVerificationTokenExpiresAt);
        builder.Property(x => x.LastLoginAt);
        builder.Property(x => x.BanReason).HasMaxLength(500);
        builder.Property(x => x.BannedAt);
        builder.Property(x => x.CompanyId);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt);
        builder.Property(x => x.DeletedAt);

        builder.HasIndex(x => x.Email).IsUnique();
        builder.HasIndex(x => x.Role);
        builder.Property(x => x.EmailOtpHash).HasMaxLength(100);
        builder.Property(x => x.EmailOtpExpiresAt);
        builder.Property(x => x.EmailOtpAttempts).HasDefaultValue(0);
    }
}
