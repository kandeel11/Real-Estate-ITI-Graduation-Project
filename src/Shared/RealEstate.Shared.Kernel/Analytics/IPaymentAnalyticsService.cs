namespace RealEstate.Shared.Kernel.Analytics;

/// <summary>
/// Implemented by Payment.Infrastructure. Exposes rent-payment revenue metrics
/// to the AI module without coupling it to PaymentDbContext.
/// </summary>
public interface IPaymentAnalyticsService
{
    Task<PaymentAnalyticsSummaryDto> GetSummaryAsync(CancellationToken ct = default);
    Task<decimal> GetRevenueInRangeAsync(DateTime from, DateTime to, CancellationToken ct = default);
}

public record PaymentAnalyticsSummaryDto(
    decimal TotalCollected,
    decimal TotalPending,
    decimal TotalLateFees,
    int OverdueCount,
    int PaidCount);
