using Microsoft.EntityFrameworkCore;
using Wallet.Application.Interfaces;
using Wallet.Domain.Entities;
using Wallet.Infrastructure.Persistence;

namespace Wallet.Infrastructure.Repositories;

public class WalletRepository : IWalletRepository
{
    private readonly WalletDbContext _context;

    public WalletRepository(WalletDbContext context) => _context = context;

    public async Task<WalletBalance?> GetByOwnerIdAsync(Guid ownerId, CancellationToken ct = default)
        => await _context.Wallets.FirstOrDefaultAsync(w => w.OwnerId == ownerId, ct);

    public async Task<WalletBalance> GetOrCreateAsync(Guid ownerId, CancellationToken ct = default)
    {
        var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.OwnerId == ownerId, ct);
        if (wallet is not null)
            return wallet;

        wallet = WalletBalance.CreateForOwner(ownerId);
        await _context.Wallets.AddAsync(wallet, ct);
        return wallet;
    }

    public async Task AddLedgerEntryAsync(LedgerEntry entry, CancellationToken ct = default)
        => await _context.LedgerEntries.AddAsync(entry, ct);

    public async Task<bool> LedgerEntryExistsAsync(Guid paymentId, CancellationToken ct = default)
        => await _context.LedgerEntries.AnyAsync(e => e.PaymentId == paymentId, ct);

    public async Task<List<LedgerEntry>> GetLedgerByOwnerIdAsync(Guid ownerId, int skip, int take, CancellationToken ct = default)
    {
        var walletId = await _context.Wallets
            .Where(w => w.OwnerId == ownerId)
            .Select(w => w.Id)
            .FirstOrDefaultAsync(ct);

        if (walletId == Guid.Empty)
            return [];

        return await _context.LedgerEntries
            .Where(e => e.WalletId == walletId)
            .OrderByDescending(e => e.OccurredAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task<int> GetLedgerCountAsync(Guid ownerId, CancellationToken ct = default)
    {
        var walletId = await _context.Wallets
            .Where(w => w.OwnerId == ownerId)
            .Select(w => w.Id)
            .FirstOrDefaultAsync(ct);

        if (walletId == Guid.Empty)
            return 0;

        return await _context.LedgerEntries.CountAsync(e => e.WalletId == walletId, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
