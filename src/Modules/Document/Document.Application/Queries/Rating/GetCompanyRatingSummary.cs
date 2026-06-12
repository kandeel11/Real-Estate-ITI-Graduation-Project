using Document.Application.Interfaces;
using MediatR;

namespace Document.Application.Queries.Rating;

/// <summary>Star distribution + totals for a company's ratings.</summary>
public record RatingBucketDto(int Stars, int Count, int Percent);

public record CompanyRatingSummaryDto(
    int Total,
    decimal Average,
    List<RatingBucketDto> Buckets);

public record GetCompanyRatingSummaryQuery(Guid CompanyId, List<Guid> PropertyIds)
    : IRequest<CompanyRatingSummaryDto>;

public class GetCompanyRatingSummaryHandler
    : IRequestHandler<GetCompanyRatingSummaryQuery, CompanyRatingSummaryDto>
{
    private readonly IRatingStatsRepository _stats;
    public GetCompanyRatingSummaryHandler(IRatingStatsRepository stats) => _stats = stats;

    public async Task<CompanyRatingSummaryDto> Handle(
        GetCompanyRatingSummaryQuery query, CancellationToken ct)
    {
        var distribution = await _stats.GetCompanyDistributionAsync(query.CompanyId, query.PropertyIds, ct);
        var total = distribution.Values.Sum();
        var average = total == 0
            ? 0m
            : Math.Round((decimal)distribution.Sum(kv => kv.Key * kv.Value) / total, 1);

        var buckets = Enumerable.Range(1, 5)
            .OrderByDescending(stars => stars)
            .Select(stars =>
            {
                var count = distribution.GetValueOrDefault(stars);
                var percent = total == 0 ? 0 : (int)Math.Round(count * 100.0 / total);
                return new RatingBucketDto(stars, count, percent);
            })
            .ToList();

        return new CompanyRatingSummaryDto(total, average, buckets);
    }
}
