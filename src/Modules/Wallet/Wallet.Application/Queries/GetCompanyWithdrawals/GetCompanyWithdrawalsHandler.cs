using MediatR;
using Wallet.Application.Interfaces;

namespace Wallet.Application.Queries.GetCompanyWithdrawals;

public class GetCompanyWithdrawalsHandler : IRequestHandler<GetCompanyWithdrawalsQuery, List<CompanyWithdrawalDto>>
{
    private readonly IWithdrawalRepository _repo;

    public GetCompanyWithdrawalsHandler(IWithdrawalRepository repo) => _repo = repo;

    public async Task<List<CompanyWithdrawalDto>> Handle(GetCompanyWithdrawalsQuery query, CancellationToken ct)
    {
        var items = await _repo.GetByCompanyIdAsync(query.CompanyId, ct);
        return items.Select(w => new CompanyWithdrawalDto(
            w.Id, w.CompanyId!.Value, w.Amount, w.CommissionAmount, w.NetAmount,
            w.Currency, w.BankAccountDetails, w.Status.ToString(),
            w.RequestedAt, w.ProcessedAt, w.Notes)).ToList();
    }
}
