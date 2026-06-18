using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuration بتاع جدول transfer_payment_details
/// Relation 1:1 مع Payment عبر PaymentId
/// </summary>
public sealed class TransferPaymentDetailsConfiguration
    : IEntityTypeConfiguration<TransferPaymentDetails>
{
    public void Configure(EntityTypeBuilder<TransferPaymentDetails> builder)
    {
        builder.ToTable("transfer_payment_details", "payment");

        builder.HasKey(x => x.PaymentId);
        builder.Property(x => x.PaymentId).ValueGeneratedNever();

        builder.Property(x => x.SenderId).IsRequired();
        builder.Property(x => x.ReceiverId).IsRequired();
        builder.Property(x => x.Purpose).HasMaxLength(255).IsRequired();

        builder.HasIndex(x => x.SenderId)
            .HasDatabaseName("IX_transfer_payment_details_sender_id");

        builder.HasIndex(x => x.ReceiverId)
            .HasDatabaseName("IX_transfer_payment_details_receiver_id");

        // العلاقة 1:1 مع Payment
        builder.HasOne<Payment.Domain.Entities.Payment>()
            .WithOne(p => p.TransferDetails)
            .HasForeignKey<TransferPaymentDetails>(x => x.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
