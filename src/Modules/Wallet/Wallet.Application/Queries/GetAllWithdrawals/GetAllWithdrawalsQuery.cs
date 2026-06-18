using MediatR;
using Wallet.Application.Queries.GetOwnerWithdrawals;

namespace Wallet.Application.Queries.GetAllWithdrawals;

public record GetAllWithdrawalsQuery : IRequest<List<WithdrawalDto>>;
