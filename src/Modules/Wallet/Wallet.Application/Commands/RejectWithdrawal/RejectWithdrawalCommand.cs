using MediatR;

namespace Wallet.Application.Commands.RejectWithdrawal;

public record RejectWithdrawalCommand(Guid WithdrawalId, string? Notes = null) : IRequest;
