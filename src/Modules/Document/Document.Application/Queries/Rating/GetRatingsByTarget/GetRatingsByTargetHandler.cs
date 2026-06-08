using Document.Application.Interfaces;
using Document.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Queries.Rating.GetRatingsByTarget;

public class GetRatingsByTargetHandler : IRequestHandler<GetRatingsByTargetQuery, List<RatingDto>>
{
    private readonly IDocumentDbContext _db;

    public GetRatingsByTargetHandler(IDocumentDbContext db) => _db = db;

    public async Task<List<RatingDto>> Handle(GetRatingsByTargetQuery query, CancellationToken ct)
    {
        var ratingsQuery = _db.Ratings
            .Where(r => r.ModerationStatus == ReviewModerationStatus.Approved
                     && r.DeletedAt == null);

        ratingsQuery = query.TargetType switch
        {
            "property" => ratingsQuery.Where(r => r.PropertyId == query.TargetId),
            "company" => ratingsQuery.Where(r => r.CompanyId == query.TargetId),
            "broker" => ratingsQuery.Where(r => r.BrokerId == query.TargetId),
            "maintenance" => ratingsQuery.Where(r => r.MaintenanceCompanyId == query.TargetId),
            _ => throw new Exception("Invalid target type")
        };

        return await ratingsQuery
            .Select(r => new RatingDto(
                r.Id, r.ReviewerId, r.RatingValue,
                r.ReviewText, r.ModerationStatus.ToString(), r.CreatedAt))
            .ToListAsync(ct);
    }
}