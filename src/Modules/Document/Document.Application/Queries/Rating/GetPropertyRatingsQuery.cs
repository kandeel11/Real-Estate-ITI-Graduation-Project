using Document.Application.Commands.Rating;
using Document.Application.DTOs;
using Document.Application.Interfaces;
using Document.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Queries.Rating;

public record GetPropertyRatingsQuery(Guid PropertyId) : IRequest<PropertyRatingsResult>;

public record PropertyRatingsResult(double AverageRating, int TotalCount, List<RatingResponse> Items);

public class GetPropertyRatingsHandler(IDocumentDbContext db)
    : IRequestHandler<GetPropertyRatingsQuery, PropertyRatingsResult>
{
    public async Task<PropertyRatingsResult> Handle(GetPropertyRatingsQuery q, CancellationToken ct)
    {
        var ratings = await db.Ratings
            .Where(r => r.PropertyId == q.PropertyId
                     && r.ModerationStatus == ReviewModerationStatus.Approved
                     && r.DeletedAt == null)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        var avg = ratings.Count > 0 ? ratings.Average(r => r.RatingValue) : 0;
        return new PropertyRatingsResult(
            Math.Round(avg, 1), ratings.Count,
            ratings.Select(CreateRatingHandler.Map).ToList());
    }
}