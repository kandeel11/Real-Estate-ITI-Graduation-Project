using Document.Application.Commands.Rating;
using Document.Application.DTOs;
using Document.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Queries.Rating;

public record GetMyRatingsQuery(Guid ReviewerId) : IRequest<List<RatingResponse>>;

public class GetMyRatingsHandler(IDocumentDbContext db)
    : IRequestHandler<GetMyRatingsQuery, List<RatingResponse>>
{
    public async Task<List<RatingResponse>> Handle(GetMyRatingsQuery q, CancellationToken ct)
    {
        var ratings = await db.Ratings
            .Where(r => r.ReviewerId == q.ReviewerId && r.DeletedAt == null)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
        return ratings.Select(CreateRatingHandler.Map).ToList();
    }
}