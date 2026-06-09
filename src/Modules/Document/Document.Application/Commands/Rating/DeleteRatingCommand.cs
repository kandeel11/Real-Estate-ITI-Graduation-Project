using Document.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Commands.Rating;

public record DeleteRatingCommand(Guid RatingId, Guid ReviewerId) : IRequest;

public class DeleteRatingHandler(IDocumentDbContext db)
    : IRequestHandler<DeleteRatingCommand>
{
    public async Task Handle(DeleteRatingCommand cmd, CancellationToken ct)
    {
        var rating = await db.Ratings.FirstOrDefaultAsync(
            r => r.Id == cmd.RatingId && r.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException("Rating not found.");

        if (rating.ReviewerId != cmd.ReviewerId)
            throw new UnauthorizedAccessException("You can only delete your own ratings.");

        rating.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}