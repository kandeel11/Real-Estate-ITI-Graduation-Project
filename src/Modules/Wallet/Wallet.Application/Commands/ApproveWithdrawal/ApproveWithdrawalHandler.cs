using MediatR;
using Wallet.Application.Interfaces;

namespace Wallet.Application.Commands.ApproveWithdrawal;

public class ApproveWithdrawalHandler : IRequestHandler<ApproveWithdrawalCommand>
{
    private readonly IWithdrawalRepository _repo;

    public ApproveWithdrawalHandler(IWithdrawalRepository repo) => _repo = repo;

    public async Task Handle(ApproveWithdrawalCommand request, CancellationToken ct)
    {
        var withdrawal = await _repo.GetByIdAsync(request.WithdrawalId, ct)
            ?? throw new KeyNotFoundException($"Withdrawal {request.WithdrawalId} not found.");

        withdrawal.Approve();
        await _repo.UpdateAsync(withdrawal, ct);
    }
}
