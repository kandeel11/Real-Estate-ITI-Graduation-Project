using MediatR;

namespace Wallet.Application.Commands.RequestWithdrawal;

public record RequestWithdrawalCommand(
    Guid OwnerId,
    decimal Amount,
    string BankAccountDetails
) : IRequest<Guid>;
