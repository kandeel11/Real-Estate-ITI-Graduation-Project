using Document.Domain.Entities;

namespace Document.Application.Interfaces;

/// <summary>
/// Read-only company-scoped inquiry lookups for dashboard stats/activity.
/// Separate from IInquiryRepository so the existing contract stays untouched.
/// </summary>
public interface IInquiryStatsRepository
{
    Task<int> CountByCompanyAsync(Guid companyId, DateTime? since = null, CancellationToken ct = default);
    Task<List<Inquiry>> GetRecentByCompanyAsync(Guid companyId, int take, CancellationToken ct = default);
    Task<List<Inquiry>> GetAllByCompanyAsync(Guid companyId, CancellationToken ct = default);
}
