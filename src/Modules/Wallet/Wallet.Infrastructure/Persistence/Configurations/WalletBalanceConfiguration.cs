using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wallet.Domain.Entities;

namespace Wallet.Infrastructure.Persistence.Configurations;

public class WalletBalanceConfiguration : IEntityTypeConfiguration<WalletBalance>
{
    public void Configure(EntityTypeBuilder<WalletBalance> builder)
    {
        builder.ToTable("wallet_balances");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).ValueGeneratedNever();

        builder.Property(w => w.OwnerId).IsRequired();
        builder.HasIndex(w => w.OwnerId).IsUnique();

        builder.Property(w => w.CompanyId);
        builder.HasIndex(w => w.CompanyId).IsUnique().HasFilter("\"CompanyId\" IS NOT NULL");

        builder.Property(w => w.Balance).HasColumnType("decimal(18,4)").IsRequired();
        builder.Property(w => w.Currency).HasMaxLength(3).IsRequired();
        builder.Property(w => w.CreatedAt).IsRequired();
        builder.Property(w => w.UpdatedAt).IsRequired();
    }
}
