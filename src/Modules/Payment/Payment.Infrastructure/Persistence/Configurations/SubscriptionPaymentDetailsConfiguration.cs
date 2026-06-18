using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuration بتاع جدول subscription_payment_details
/// Relation 1:1 مع Payment عبر PaymentId — بدون أي FK لجداول Subscription module
/// </summary>
public sealed class SubscriptionPaymentDetailsConfiguration
    : IEntityTypeConfiguration<SubscriptionPaymentDetails>
{
    public void Configure(EntityTypeBuilder<SubscriptionPaymentDetails> builder)
    {
        builder.ToTable("subscription_payment_details", "payment");

        builder.HasKey(x => x.PaymentId);
        builder.Property(x => x.PaymentId).ValueGeneratedNever();

        builder.Property(x => x.SubscriptionId).IsRequired();
        builder.Property(x => x.PlanId).IsRequired();

        builder.Property(x => x.BillingCycle)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(x => x.SubscriptionId)
            .HasDatabaseName("IX_subscription_payment_details_subscription_id");

        // العلاقة 1:1 مع Payment
        builder.HasOne<Payment.Domain.Entities.Payment>()
            .WithOne(p => p.SubscriptionDetails)
            .HasForeignKey<SubscriptionPaymentDetails>(x => x.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
