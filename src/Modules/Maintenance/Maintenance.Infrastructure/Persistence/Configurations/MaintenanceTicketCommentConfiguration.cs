using Maintenance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maintenance.Infrastructure.Persistence.Configurations;

public class MaintenanceTicketCommentConfiguration : IEntityTypeConfiguration<MaintenanceTicketComment>
{
    public void Configure(EntityTypeBuilder<MaintenanceTicketComment> b)
    {
        b.ToTable("maintenance_ticket_comments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.TicketId).IsRequired();
        b.Property(x => x.UserId).IsRequired();
        b.Property(x => x.UserRole).IsRequired().HasConversion<string>();
        b.Property(x => x.Content).IsRequired();
        b.Property(x => x.IsInternal).IsRequired().HasDefaultValue(false);
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.UpdatedAt);
        b.Property(x => x.DeletedAt);
        b.HasQueryFilter(x => x.DeletedAt == null);
        b.HasIndex(x => x.TicketId);
    }
}