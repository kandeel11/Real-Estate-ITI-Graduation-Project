using MediatR;
using Wallet.Application.Interfaces;
using Wallet.Application.Queries.GetWalletBalance;

namespace Wallet.Application.Queries.GetCompanyWalletBalance;

public class GetCompanyWalletBalanceHandler : IRequestHandler<GetCompanyWalletBalanceQuery, WalletBalanceDto?>
{
    private readonly IWalletRepository _repo;

    public GetCompanyWalletBalanceHandler(IWalletRepository repo) => _repo = repo;

    public async Task<WalletBalanceDto?> Handle(GetCompanyWalletBalanceQuery request, CancellationToken ct)
    {
        var wallet = await _repo.GetByCompanyIdAsync(request.CompanyId, ct);
        if (wallet is null) return null;

        return new WalletBalanceDto(wallet.Id, wallet.OwnerId, wallet.Balance, wallet.Currency, wallet.UpdatedAt);
    }
}
