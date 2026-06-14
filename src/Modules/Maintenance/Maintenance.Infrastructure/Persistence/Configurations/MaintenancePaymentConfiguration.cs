using Maintenance.Domain.Entities;
using Maintenance.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maintenance.Infrastructure.Persistence.Configurations;

public class MaintenancePaymentConfiguration : IEntityTypeConfiguration<MaintenancePayment>
{
    public void Configure(EntityTypeBuilder<MaintenancePayment> b)
    {
        b.ToTable("maintenance_payments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.TicketId).IsRequired();
        // Match the parent ticket's soft-delete filter so this required dependent
        // is filtered out together with its ticket.
        b.HasQueryFilter(x => x.Ticket.DeletedAt == null);
        b.HasIndex(x => x.TicketId).IsUnique();
        b.Property(x => x.PayerId).IsRequired();
        b.Property(x => x.VendorId).IsRequired();
        b.Property(x => x.Amount).IsRequired().HasPrecision(10, 2);
        b.Property(x => x.Currency).IsRequired().HasMaxLength(10).HasDefaultValue("SAR");
        b.Property(x => x.Status).IsRequired().HasConversion<string>().HasDefaultValue(MaintenancePaymentStatus.Pending);
        b.Property(x => x.PaymentMethod).HasMaxLength(100);
        b.Property(x => x.TransactionId).HasMaxLength(255);
        b.Property(x => x.Notes).HasMaxLength(500);
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.UpdatedAt);
    }
}