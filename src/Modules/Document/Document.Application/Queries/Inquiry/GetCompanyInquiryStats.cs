using Document.Application.Interfaces;
using MediatR;

namespace Document.Application.Queries.Inquiry;

/// <summary>Inquiry counters + recent items for a company's dashboard.</summary>
public record CompanyInquiryStatsDto(
    int TotalInquiries,
    int MonthlyInquiries,
    List<CompanyInquiryItemDto> Recent);

public record CompanyInquiryItemDto(
    Guid Id,
    Guid PropertyId,
    string Status,
    DateTime CreatedAt);

public record GetCompanyInquiryStatsQuery(Guid CompanyId, int RecentTake = 5)
    : IRequest<CompanyInquiryStatsDto>;

public class GetCompanyInquiryStatsHandler
    : IRequestHandler<GetCompanyInquiryStatsQuery, CompanyInquiryStatsDto>
{
    private readonly IInquiryStatsRepository _stats;
    public GetCompanyInquiryStatsHandler(IInquiryStatsRepository stats) => _stats = stats;

    public async Task<CompanyInquiryStatsDto> Handle(
        GetCompanyInquiryStatsQuery query, CancellationToken ct)
    {
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var total = await _stats.CountByCompanyAsync(query.CompanyId, null, ct);
        var monthly = await _stats.CountByCompanyAsync(query.CompanyId, monthStart, ct);
        var recent = await _stats.GetRecentByCompanyAsync(query.CompanyId, query.RecentTake, ct);

        return new CompanyInquiryStatsDto(
            total,
            monthly,
            recent.Select(i => new CompanyInquiryItemDto(
                i.Id, i.PropertyId, i.Status.ToString(), i.CreatedAt)).ToList());
    }
}
