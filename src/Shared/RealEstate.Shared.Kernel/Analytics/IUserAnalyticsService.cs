namespace RealEstate.Shared.Kernel.Analytics;

/// <summary>
/// Implemented by Identity.Infrastructure. Exposes user and company count metrics
/// to the AI module without a direct dependency on IdentityDbContext.
/// </summary>
public interface IUserAnalyticsService
{
    Task<UserAnalyticsSummaryDto> GetSummaryAsync(CancellationToken ct = default);
    Task<int> GetNewUsersCountAsync(DateTime from, DateTime to, CancellationToken ct = default);
}

public record UserAnalyticsSummaryDto(
    int TotalUsers,
    int TotalCompanies,
    int TotalBrokers);
