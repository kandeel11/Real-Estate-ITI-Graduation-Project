using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payment.Domain.Entities;
using Payment.Domain.Enums;


namespace Payment.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuration بتاع جدول payments — Payment هو single source of truth
/// لكل المعاملات المالية (Subscription, Rent, Transfer, Refund, Deposit, Withdrawal)
/// </summary>
public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment.Domain.Entities.Payment>
{
    public void Configure(EntityTypeBuilder<Payment.Domain.Entities.Payment> builder)
    {
        builder.ToTable("payments", "payment");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedNever(); // الـ ID بيتولد في الـ Domain (Guid.NewGuid())

        builder.Property(p => p.Amount)
            .HasColumnType("decimal(12,2)")
            .IsRequired();

        builder.Property(p => p.Currency)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.PaymentMethod)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.PaymentType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(PaymentType.Rent)
            .IsRequired();

        builder.Property(p => p.TransactionId)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(p => p.CheckoutSessionId)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(p => p.CheckoutUrl)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(p => p.DueDate)
            .IsRequired();

        builder.Property(p => p.PaidAt)
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

        // Computed/derived navigation-based properties are not mapped
        builder.Ignore(p => p.LeaseId);

        // Index على PaymentType علشان نفلتر بسهولة بين الأنواع المختلفة
        builder.HasIndex(p => p.PaymentType)
            .HasDatabaseName("IX_payments_payment_type");

        // Index على Status علشان queries بتاعة الدفعات المتأخرة
        builder.HasIndex(p => p.Status)
            .HasDatabaseName("IX_payments_status");

        // Index مركب: DueDate علشان queries المتأخرة
        builder.HasIndex(p => p.DueDate)
            .HasDatabaseName("IX_payments_due_date");
    }
}
