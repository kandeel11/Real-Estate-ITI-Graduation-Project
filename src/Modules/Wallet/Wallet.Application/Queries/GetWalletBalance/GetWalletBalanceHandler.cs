using MediatR;
using Wallet.Application.Interfaces;

namespace Wallet.Application.Queries.GetWalletBalance;

public class GetWalletBalanceHandler : IRequestHandler<GetWalletBalanceQuery, WalletBalanceDto?>
{
    private readonly IWalletRepository _repo;

    public GetWalletBalanceHandler(IWalletRepository repo) => _repo = repo;

    public async Task<WalletBalanceDto?> Handle(GetWalletBalanceQuery request, CancellationToken cancellationToken)
    {
        var wallet = await _repo.GetByOwnerIdAsync(request.OwnerId, cancellationToken);
        if (wallet is null) return null;

        return new WalletBalanceDto(wallet.Id, wallet.OwnerId, wallet.Balance, wallet.Currency, wallet.UpdatedAt);
    }
}
