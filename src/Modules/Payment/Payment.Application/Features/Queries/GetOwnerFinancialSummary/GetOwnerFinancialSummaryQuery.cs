using MediatR;

namespace Payment.Application.Features.Queries.GetOwnerFinancialSummary;

public record GetOwnerFinancialSummaryQuery(Guid OwnerId) : IRequest<OwnerFinancialSummaryDto>;

public record MonthlyRevenueDto(string Month, int Year, decimal Revenue, int Count);

public record OwnerFinancialSummaryDto(
    decimal TotalRevenue,
    decimal TotalPending,
    decimal TotalPlatformFee,
    decimal TotalLateFees,
    int     TotalPaidCount,
    int     TotalPendingCount,
    List<MonthlyRevenueDto> Monthly
);
