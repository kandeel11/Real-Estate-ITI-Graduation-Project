using Maintenance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maintenance.Infrastructure.Persistence.Configurations;

public class TicketStatusHistoryConfiguration : IEntityTypeConfiguration<TicketStatusHistory>
{
    public void Configure(EntityTypeBuilder<TicketStatusHistory> b)
    {
        b.ToTable("maintenance_ticket_status_history");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.TicketId).IsRequired();
        // Match the parent ticket's soft-delete filter so this required dependent
        // is filtered out together with its ticket.
        b.HasQueryFilter(x => x.Ticket.DeletedAt == null);
        b.Property(x => x.FromStatus).IsRequired().HasConversion<string>();
        b.Property(x => x.ToStatus).IsRequired().HasConversion<string>();
        b.Property(x => x.ChangedBy).IsRequired().HasMaxLength(255);
        b.Property(x => x.Notes).HasMaxLength(500);
        b.Property(x => x.CreatedAt).IsRequired();
        b.HasIndex(x => x.TicketId);
    }
}