using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subscription.Domain.Entities;
using Subscription.Domain.Enums;

namespace Subscription.Infrastructure.Persistence.Configurations;

public class SubscriptionPaymentConfiguration : IEntityTypeConfiguration<SubscriptionPayment>
{
    public void Configure(EntityTypeBuilder<SubscriptionPayment> builder)
    {
        builder.ToTable("subscription_payments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SubscriptionId).IsRequired();
        builder.Property(x => x.Amount).IsRequired().HasPrecision(10, 2);
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(10);
        builder.Property(x => x.PaymentMethod).IsRequired().HasMaxLength(100);
        builder.Property(x => x.TransactionId).HasMaxLength(255);
        builder.Property(x => x.Status).IsRequired().HasConversion<string>();
        builder.Property(x => x.PaidAt);
        builder.Property(x => x.DueDate).IsRequired();
        builder.Property(x => x.InvoiceUrl);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt);
        builder.Property(x => x.DeletedAt);

        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.HasIndex(x => x.TransactionId).IsUnique();
        builder.HasIndex(x => x.SubscriptionId);
        builder.HasIndex(x => x.Status);
    }
}
