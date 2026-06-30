namespace Maintenance.Application.Interfaces;

/// <summary>
/// Bridges the Wallet module into Maintenance so the command layer can
/// deduct wallet funds without a cross-module reference.
/// Implemented by WalletDeductionAdapter in the Host layer.
/// </summary>
public interface IWalletDeductionService
{
    Task<decimal> GetOwnerBalanceAsync(Guid ownerId, CancellationToken ct = default);
    Task<decimal> GetCompanyBalanceAsync(Guid companyId, CancellationToken ct = default);
    Task DebitOwnerAsync(Guid ownerId, Guid paymentId, decimal amount, string description, CancellationToken ct = default);
    Task DebitCompanyAsync(Guid companyId, Guid paymentId, decimal amount, string description, CancellationToken ct = default);
}
