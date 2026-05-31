using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Document.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Document.Domain.Entities.Notification>
{
    public void Configure(EntityTypeBuilder<Document.Domain.Entities.Notification> b)
    {
        b.ToTable("notifications");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.UserId).IsRequired();
        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(100).IsRequired();
        b.Property(x => x.Title).HasMaxLength(255).IsRequired();
        b.Property(x => x.Body).HasColumnType("nvarchar(max)").IsRequired();
        b.Property(x => x.RelatedEntityType).HasMaxLength(100);
        b.Property(x => x.IsRead).HasDefaultValue(false);
        b.Property(x => x.CreatedAt).IsRequired();
        b.HasIndex(x => new { x.UserId, x.IsRead });
    }
}