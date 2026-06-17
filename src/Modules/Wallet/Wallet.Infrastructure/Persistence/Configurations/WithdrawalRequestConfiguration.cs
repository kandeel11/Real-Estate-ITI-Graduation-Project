using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wallet.Domain.Entities;

namespace Wallet.Infrastructure.Persistence.Configurations;

public class WithdrawalRequestConfiguration : IEntityTypeConfiguration<WithdrawalRequest>
{
    public void Configure(EntityTypeBuilder<WithdrawalRequest> builder)
    {
        builder.ToTable("wallet_withdrawal_requests");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).ValueGeneratedNever();

        builder.Property(w => w.OwnerId).IsRequired();
        builder.HasIndex(w => w.OwnerId);

        builder.Property(w => w.Amount).HasColumnType("decimal(18,4)").IsRequired();
        builder.Property(w => w.Currency).HasMaxLength(3).IsRequired();
        builder.Property(w => w.BankAccountDetails).HasMaxLength(1000).IsRequired();
        builder.Property(w => w.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(w => w.RequestedAt).IsRequired();
        builder.Property(w => w.ProcessedAt);
        builder.Property(w => w.Notes).HasMaxLength(500);
    }
}
