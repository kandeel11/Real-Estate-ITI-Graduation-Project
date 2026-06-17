using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuration بتاع جدول rent_payment_details
/// Relation 1:1 مع Payment عبر PaymentId — بدون FK لـ Property/Identity modules
/// </summary>
public sealed class RentPaymentDetailsConfiguration : IEntityTypeConfiguration<RentPaymentDetails>
{
    public void Configure(EntityTypeBuilder<RentPaymentDetails> builder)
    {
        builder.ToTable("rent_payment_details", "payment");

        builder.HasKey(x => x.PaymentId);
        builder.Property(x => x.PaymentId).ValueGeneratedNever();

        builder.Property(x => x.LeaseId).IsRequired();
        builder.Property(x => x.PropertyId).IsRequired();
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.LandlordId).IsRequired();

        builder.HasIndex(x => x.LeaseId)
            .HasDatabaseName("IX_rent_payment_details_lease_id");

        builder.HasIndex(x => new { x.LeaseId, x.PaymentId })
            .HasDatabaseName("IX_rent_payment_details_lease_payment");

        // العلاقة 1:1 مع Payment
        builder.HasOne<Payment.Domain.Entities.Payment>()
            .WithOne(p => p.RentDetails)
            .HasForeignKey<RentPaymentDetails>(x => x.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
