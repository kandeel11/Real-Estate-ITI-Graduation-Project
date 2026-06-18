using MediatR;
using Wallet.Application.Interfaces;

namespace Wallet.Application.Commands.RejectWithdrawal;

public class RejectWithdrawalHandler : IRequestHandler<RejectWithdrawalCommand>
{
    private readonly IWithdrawalRepository _repo;

    public RejectWithdrawalHandler(IWithdrawalRepository repo) => _repo = repo;

    public async Task Handle(RejectWithdrawalCommand request, CancellationToken ct)
    {
        var withdrawal = await _repo.GetByIdAsync(request.WithdrawalId, ct)
            ?? throw new KeyNotFoundException($"Withdrawal {request.WithdrawalId} not found.");

        withdrawal.Reject(request.Notes);
        await _repo.UpdateAsync(withdrawal, ct);
    }
}
