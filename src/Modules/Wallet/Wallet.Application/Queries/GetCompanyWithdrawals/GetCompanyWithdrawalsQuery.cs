using MediatR;
using Wallet.Application.Queries.GetOwnerWithdrawals;

namespace Wallet.Application.Queries.GetCompanyWithdrawals;

public record GetCompanyWithdrawalsQuery(Guid CompanyId) : IRequest<List<CompanyWithdrawalDto>>;

public record CompanyWithdrawalDto(
    Guid Id,
    Guid CompanyId,
    decimal Amount,
    decimal CommissionAmount,
    decimal NetAmount,
    string Currency,
    string BankAccountDetails,
    string Status,
    DateTime RequestedAt,
    DateTime? ProcessedAt,
    string? Notes);
