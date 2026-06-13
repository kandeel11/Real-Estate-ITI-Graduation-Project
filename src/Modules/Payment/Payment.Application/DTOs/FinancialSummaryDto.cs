
namespace Payment.Application.DTOs;

public sealed record FinancialSummaryDto(
        decimal TotalCollected,
        decimal TotalPending,
        decimal TotalLateFees,
        int TotalOverduePayments
);


