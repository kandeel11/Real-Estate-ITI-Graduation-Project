using MediatR;
using Wallet.Application.Interfaces;
using Wallet.Application.Queries.GetOwnerWithdrawals;

namespace Wallet.Application.Queries.GetAllWithdrawals;

public class GetAllWithdrawalsHandler : IRequestHandler<GetAllWithdrawalsQuery, List<WithdrawalDto>>
{
    private readonly IWithdrawalRepository _repo;

    public GetAllWithdrawalsHandler(IWithdrawalRepository repo) => _repo = repo;

    public async Task<List<WithdrawalDto>> Handle(GetAllWithdrawalsQuery query, CancellationToken ct)
    {
        var items = await _repo.GetAllAsync(ct);
        return items.Select(w => new WithdrawalDto(
            w.Id, w.OwnerId, w.Amount, w.CommissionAmount, w.NetAmount,
            w.Currency, w.BankAccountDetails,
            w.Status.ToString(), w.RequestedAt, w.ProcessedAt, w.Notes)).ToList();
    }
}
