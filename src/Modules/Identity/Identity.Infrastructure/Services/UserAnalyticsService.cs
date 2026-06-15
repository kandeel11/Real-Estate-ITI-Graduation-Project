using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using RealEstate.Shared.Kernel.Analytics;

namespace Identity.Infrastructure.Services;

public sealed class UserAnalyticsService : IUserAnalyticsService
{
    private readonly IdentityDbContext _db;

    public UserAnalyticsService(IdentityDbContext db) => _db = db;

    public async Task<UserAnalyticsSummaryDto> GetSummaryAsync(CancellationToken ct = default)
    {
        var totalUsers     = await _db.Users.AsNoTracking().CountAsync(ct);
        var totalCompanies = await _db.Companies.AsNoTracking().CountAsync(ct);
        var totalBrokers   = await _db.Brokers.AsNoTracking().CountAsync(ct);

        return new UserAnalyticsSummaryDto(totalUsers, totalCompanies, totalBrokers);
    }

    public async Task<int> GetNewUsersCountAsync(DateTime from, DateTime to, CancellationToken ct = default)
        => await _db.Users
            .AsNoTracking()
            .CountAsync(u => u.CreatedAt >= from && u.CreatedAt <= to, ct);
}
