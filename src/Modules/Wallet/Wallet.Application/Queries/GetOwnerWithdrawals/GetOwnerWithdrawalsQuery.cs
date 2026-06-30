using MediatR;

namespace Wallet.Application.Queries.GetOwnerWithdrawals;

public record GetOwnerWithdrawalsQuery(Guid OwnerId) : IRequest<List<WithdrawalDto>>;

public record WithdrawalDto(
    Guid Id,
    Guid OwnerId,
    decimal Amount,
    decimal CommissionAmount,
    decimal NetAmount,
    string Currency,
    string BankAccountDetails,
    string Status,
    DateTime RequestedAt,
    DateTime? ProcessedAt,
    string? Notes);
