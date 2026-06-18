using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payment.Domain.Entities;
using Payment.Domain.Enums;

namespace Payment.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuration بتاع جدول RentPayment
/// بنحدد فيه: schema، اسم الجدول، أسماء الأعمدة، القيود، والـ defaults
/// 
/// نفس النمط المستخدم في MaintenancePaymentConfiguration و SubscriptionPaymentConfiguration
/// </summary>
public sealed class RentPaymentConfiguration : IEntityTypeConfiguration<RentPayment>
{
    public void Configure(EntityTypeBuilder<RentPayment> builder)
    {
        // Schema = "payment" و اسم الجدول = "rent_payments"
        builder.ToTable("rent_payments", "payment");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedNever(); // الـ ID بيتولد في الـ Domain (Guid.NewGuid())

        builder.Property(p => p.LeaseId)
            .IsRequired();
        // ملاحظة: لا يوجد FK لأن LeaseId تابع لـ module منفصل

        builder.Property(p => p.Amount)
            .HasColumnType("decimal(12,2)")
            .IsRequired();

        builder.Property(p => p.DueDate)
            .IsRequired();

        builder.Property(p => p.PaidAt)
            .IsRequired(false);

        builder.Property(p => p.Status)
            .HasConversion<string>()   // نخزن الـ enum كـ string في الداتابيز
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

        // ── NEW: Subscription support columns ─────────────────
        builder.Property(p => p.PaymentType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(PaymentType.Rent)
            .IsRequired();

        builder.Property(p => p.CheckoutSessionId)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(p => p.CheckoutUrl)
            .HasMaxLength(1000)
            .IsRequired(false);

        // Index على PaymentType علشان نفلتر بسهولة بين Rent و Subscription
        builder.HasIndex(p => p.PaymentType)
            .HasDatabaseName("IX_rent_payments_payment_type");

        // Index على LeaseId علشان queries بتاعة lease تكون سريعة
        builder.HasIndex(p => p.LeaseId)
            .HasDatabaseName("IX_rent_payments_lease_id");

        // Index على Status علشان queries بتاعة الدفعات المتأخرة
        builder.HasIndex(p => p.Status)
            .HasDatabaseName("IX_rent_payments_status");

        // Index مركب: LeaseId + DueDate علشان نتحقق من التكرار بسرعة
        builder.HasIndex(p => new { p.LeaseId, p.DueDate })
            .HasDatabaseName("IX_rent_payments_lease_duedate");
    }
}
