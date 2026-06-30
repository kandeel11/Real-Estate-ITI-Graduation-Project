using Maintenance.Application.Interfaces;
using Wallet.Application.Interfaces;
using Wallet.Domain.Entities;

namespace RealEstate.Host.Adapters;

/// <summary>
/// Bridges the Wallet module into Maintenance.Application's IWalletDeductionService.
/// Lives in Host so neither module holds a cross-module reference.
/// </summary>
public sealed class WalletDeductionAdapter(IWalletRepository walletRepo) : IWalletDeductionService
{
    public async Task<decimal> GetOwnerBalanceAsync(Guid ownerId, CancellationToken ct)
    {
        var wallet = await walletRepo.GetByOwnerIdAsync(ownerId, ct);
        return wallet?.Balance ?? 0m;
    }

    public async Task<decimal> GetCompanyBalanceAsync(Guid companyId, CancellationToken ct)
    {
        var wallet = await walletRepo.GetByCompanyIdAsync(companyId, ct);
        return wallet?.Balance ?? 0m;
    }

    public async Task DebitOwnerAsync(Guid ownerId, Guid paymentId, decimal amount, string description, CancellationToken ct)
    {
        var wallet = await walletRepo.GetOrCreateAsync(ownerId, ct);
        if (wallet.Balance < amount)
            throw new InvalidOperationException("Insufficient wallet balance.");
        wallet.Debit(amount);
        var entry = LedgerEntry.CreateDebit(wallet.Id, paymentId, amount, description);
        await walletRepo.AddLedgerEntryAsync(entry, ct);
        await walletRepo.SaveChangesAsync(ct);
    }

    public async Task DebitCompanyAsync(Guid companyId, Guid paymentId, decimal amount, string description, CancellationToken ct)
    {
        var wallet = await walletRepo.GetOrCreateForCompanyAsync(companyId, ct);
        if (wallet.Balance < amount)
            throw new InvalidOperationException("Insufficient wallet balance.");
        wallet.Debit(amount);
        var entry = LedgerEntry.CreateDebit(wallet.Id, paymentId, amount, description);
        await walletRepo.AddLedgerEntryAsync(entry, ct);
        await walletRepo.SaveChangesAsync(ct);
    }
}
