using MediatR;

namespace Wallet.Application.Commands.ApproveWithdrawal;

public record ApproveWithdrawalCommand(Guid WithdrawalId) : IRequest;
