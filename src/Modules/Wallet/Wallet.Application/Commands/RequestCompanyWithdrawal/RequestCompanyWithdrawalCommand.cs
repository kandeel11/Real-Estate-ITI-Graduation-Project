using MediatR;

namespace Wallet.Application.Commands.RequestCompanyWithdrawal;

public record RequestCompanyWithdrawalCommand(
    Guid CompanyId,
    decimal Amount,
    string BankAccountDetails) : IRequest<Guid>;
