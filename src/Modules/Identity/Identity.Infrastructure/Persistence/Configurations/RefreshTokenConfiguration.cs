using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Token).IsRequired().HasMaxLength(500);
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.RevokedAt);
        builder.Property(x => x.ReplacedByToken).HasMaxLength(500);
        builder.Property(x => x.CreatedByIp).HasMaxLength(45);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.Token).IsUnique();
        builder.HasIndex(x => x.UserId);
    }
}
