using Maintenance.Domain.Entities;
using Maintenance.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maintenance.Infrastructure.Persistence.Configurations;

public class MaintenanceTicketConfiguration : IEntityTypeConfiguration<MaintenanceTicket>
{
    public void Configure(EntityTypeBuilder<MaintenanceTicket> b)
    {
        b.ToTable("maintenance_tickets");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.HasQueryFilter(x => x.DeletedAt == null);

        b.Property(x => x.TicketNumber).HasMaxLength(20);
        b.HasIndex(x => x.TicketNumber).IsUnique();
        b.Property(x => x.PropertyId).IsRequired();
        b.Property(x => x.TenantId).IsRequired();
        b.Property(x => x.OwnerId).IsRequired();
        b.Property(x => x.LeaseId);
        b.Property(x => x.Title).IsRequired().HasMaxLength(255);
        b.Property(x => x.Description).IsRequired();
        b.Property(x => x.PropertyAddress).HasMaxLength(500);
        b.Property(x => x.TenantEmail).HasMaxLength(255);
        b.Property(x => x.TenantPhone).HasMaxLength(50);
        // No DB default: the domain factory always sets Priority explicitly. A DB default
        // here causes EF to omit the value on insert when it equals the CLR default
        // (Emergency), silently downgrading Emergency tickets to the default. (EF warning 20601)
        b.Property(x => x.Priority).IsRequired().HasConversion<string>();
        b.Property(x => x.Status).IsRequired().HasConversion<string>().HasDefaultValue(TicketStatus.Draft);
        b.Property(x => x.ResponsibleParty).HasConversion<string>();
        b.Property(x => x.ResolutionMethod).HasConversion<string>();
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.UpdatedAt);
        b.Property(x => x.DeletedAt);

        b.Property(x => x.RoutedToCompanyId);
        b.Property(x => x.ParentTicketId);

        b.Property(x => x.DisputeReason);
        b.Property(x => x.DisputedById);
        b.Property(x => x.DisputedByRole).HasMaxLength(100);
        b.Property(x => x.DisputeDate);
        b.Property(x => x.AdminNotes);

        b.HasIndex(x => x.TenantId);
        b.HasIndex(x => x.OwnerId);
        b.HasIndex(x => x.PropertyId);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.RoutedToCompanyId);

        b.HasMany(x => x.Attachments).WithOne(a => a.Ticket)
            .HasForeignKey(a => a.TicketId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Comments).WithOne(c => c.Ticket)
            .HasForeignKey(c => c.TicketId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.StatusHistory).WithOne(h => h.Ticket)
            .HasForeignKey(h => h.TicketId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.AiEvaluation).WithOne(e => e.Ticket)
            .HasForeignKey<TicketAiEvaluation>(e => e.TicketId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.CurrentVendorAssignment).WithOne(a => a.Ticket)
            .HasForeignKey<TicketVendorAssignment>(a => a.TicketId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Payment).WithOne(p => p.Ticket)
            .HasForeignKey<MaintenancePayment>(p => p.TicketId).OnDelete(DeleteBehavior.Cascade);
    }
}