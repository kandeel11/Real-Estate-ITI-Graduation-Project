using MediatR;
using Wallet.Application.Interfaces;

namespace Wallet.Application.Queries.GetOwnerWithdrawals;

public class GetOwnerWithdrawalsHandler : IRequestHandler<GetOwnerWithdrawalsQuery, List<WithdrawalDto>>
{
    private readonly IWithdrawalRepository _repo;

    public GetOwnerWithdrawalsHandler(IWithdrawalRepository repo) => _repo = repo;

    public async Task<List<WithdrawalDto>> Handle(GetOwnerWithdrawalsQuery query, CancellationToken ct)
    {
        var items = await _repo.GetByOwnerIdAsync(query.OwnerId, ct);
        return items.Select(w => new WithdrawalDto(
            w.Id, w.OwnerId, w.Amount, w.CommissionAmount, w.NetAmount,
            w.Currency, w.BankAccountDetails,
            w.Status.ToString(), w.RequestedAt, w.ProcessedAt, w.Notes)).ToList();
    }
}
