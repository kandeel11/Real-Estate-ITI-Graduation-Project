using MediatR;

namespace Wallet.Application.Queries.GetWalletBalance;

public record GetWalletBalanceQuery(Guid OwnerId) : IRequest<WalletBalanceDto?>;

public record WalletBalanceDto(Guid WalletId, Guid OwnerId, decimal Balance, string Currency, DateTime UpdatedAt);
