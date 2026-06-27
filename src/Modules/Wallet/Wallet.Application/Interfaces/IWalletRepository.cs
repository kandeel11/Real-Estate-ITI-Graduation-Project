using Wallet.Domain.Entities;

namespace Wallet.Application.Interfaces;

public interface IWalletRepository
{
    Task<WalletBalance?> GetByOwnerIdAsync(Guid ownerId, CancellationToken ct = default);
    Task<WalletBalance> GetOrCreateAsync(Guid ownerId, CancellationToken ct = default);
    Task<WalletBalance?> GetByCompanyIdAsync(Guid companyId, CancellationToken ct = default);
    Task<WalletBalance> GetOrCreateForCompanyAsync(Guid companyId, CancellationToken ct = default);
    Task AddLedgerEntryAsync(LedgerEntry entry, CancellationToken ct = default);
    Task<bool> LedgerEntryExistsAsync(Guid paymentId, CancellationToken ct = default);
    Task<bool> CompanyLedgerEntryExistsAsync(Guid paymentId, CancellationToken ct = default);
    Task<List<LedgerEntry>> GetLedgerByOwnerIdAsync(Guid ownerId, int skip, int take, CancellationToken ct = default);
    Task<int> GetLedgerCountAsync(Guid ownerId, CancellationToken ct = default);
    Task<List<LedgerEntry>> GetLedgerByCompanyIdAsync(Guid companyId, int skip, int take, CancellationToken ct = default);
    Task<int> GetLedgerCountForCompanyAsync(Guid companyId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
