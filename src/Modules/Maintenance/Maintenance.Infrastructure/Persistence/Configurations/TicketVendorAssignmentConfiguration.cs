using Maintenance.Domain.Entities;
using Maintenance.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maintenance.Infrastructure.Persistence.Configurations;

public class TicketVendorAssignmentConfiguration : IEntityTypeConfiguration<TicketVendorAssignment>
{
    public void Configure(EntityTypeBuilder<TicketVendorAssignment> b)
    {
        b.ToTable("maintenance_ticket_vendor_assignments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.TicketId).IsRequired();
        b.Property(x => x.VendorId).IsRequired();
        b.Property(x => x.AssignedById).IsRequired();
        b.Property(x => x.Status).IsRequired().HasConversion<string>().HasDefaultValue(AssignmentStatus.Pending);
        b.Property(x => x.RejectionReason).HasMaxLength(500);
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.UpdatedAt);
        b.HasIndex(x => x.TicketId);
    }
}