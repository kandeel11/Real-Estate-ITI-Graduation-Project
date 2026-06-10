using Document.Application.DTOs;
using Document.Application.Interfaces;
using Document.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Commands.Rating;

public record UpdateRatingCommand(
    Guid RatingId, Guid ReviewerId, int RatingValue, string? ReviewText) : IRequest<RatingResponse>;

public class UpdateRatingHandler(IDocumentDbContext db)
    : IRequestHandler<UpdateRatingCommand, RatingResponse>
{
    public async Task<RatingResponse> Handle(UpdateRatingCommand cmd, CancellationToken ct)
    {
        if (cmd.RatingValue < 1 || cmd.RatingValue > 5)
            throw new ArgumentException("Rating must be between 1 and 5.");

        var rating = await db.Ratings.FirstOrDefaultAsync(
            r => r.Id == cmd.RatingId && r.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException("Rating not found.");

        if (rating.ReviewerId != cmd.ReviewerId)
            throw new UnauthorizedAccessException("You can only edit your own ratings.");

        if (rating.ModerationStatus != ReviewModerationStatus.Pending)
            throw new InvalidOperationException("Cannot edit a rating after it has been reviewed.");

        if ((DateTime.UtcNow - rating.CreatedAt).TotalHours > 24)
            throw new InvalidOperationException("Ratings can only be edited within 24 hours of submission.");

        rating.RatingValue = cmd.RatingValue;
        rating.ReviewText = cmd.ReviewText;
        rating.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return CreateRatingHandler.Map(rating);
    }
}