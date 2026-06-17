using Wallet.Domain.Entities;

namespace Wallet.Application.Interfaces;

public interface IWithdrawalRepository
{
    Task<Guid> CreateAsync(WithdrawalRequest request, CancellationToken ct = default);
    Task<List<WithdrawalRequest>> GetByOwnerIdAsync(Guid ownerId, CancellationToken ct = default);
}
