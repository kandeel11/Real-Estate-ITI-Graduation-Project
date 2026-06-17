using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wallet.Domain.Entities;

namespace Wallet.Infrastructure.Persistence.Configurations;

public class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ToTable("wallet_ledger_entries");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.WalletId).IsRequired();
        builder.HasIndex(e => e.WalletId);

        builder.Property(e => e.PaymentId).IsRequired();
        // Idempotency: one credit entry per PaymentId
        builder.HasIndex(e => e.PaymentId).IsUnique();

        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.Amount).HasColumnType("decimal(18,4)").IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500).IsRequired();
        builder.Property(e => e.OccurredAt).IsRequired();
    }
}
