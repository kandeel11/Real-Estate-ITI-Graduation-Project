using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payment.Domain.Entities;
using Payment.Domain.Enums;

namespace Payment.Infrastructure.Persistence.Configurations;
public sealed class RentPaymentConfiguration : IEntityTypeConfiguration<RentPayment>
{
    public void Configure(EntityTypeBuilder<RentPayment> builder)
    {
        builder.ToTable("rent_payments", "payment");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedNever(); 

        builder.Property(p => p.LeaseId)
            .IsRequired();

        builder.Property(p => p.Amount)
            .HasColumnType("decimal(12,2)")
            .IsRequired();

        builder.Property(p => p.DueDate)
            .IsRequired();

        builder.Property(p => p.PaidAt)
            .IsRequired(false);

        builder.Property(p => p.Status)
            .HasConversion<string>()   
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.PaymentMethod)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(p => p.TransactionId)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(p => p.LateFee)
            .HasColumnType("decimal(10,2)")
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(p => p.AiPredictedLate)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(p => p.ReceiptUrl)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(p => p.Notes)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .IsRequired();

        builder.HasIndex(p => p.LeaseId)
            .HasDatabaseName("IX_rent_payments_lease_id");

        builder.HasIndex(p => p.Status)
            .HasDatabaseName("IX_rent_payments_status");

        builder.HasIndex(p => new { p.LeaseId, p.DueDate })
            .HasDatabaseName("IX_rent_payments_lease_duedate");
    }
}