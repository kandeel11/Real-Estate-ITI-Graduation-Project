using MediatR;
using Wallet.Application.Queries.GetWalletBalance;

namespace Wallet.Application.Queries.GetCompanyWalletBalance;

public record GetCompanyWalletBalanceQuery(Guid CompanyId) : IRequest<WalletBalanceDto?>;
