using Document.Application.Interfaces;
using Document.Domain.Enums;
using MediatR;

namespace Document.Application.Commands.Rating.ModerateRating;

public class ModerateRatingHandler : IRequestHandler<ModerateRatingCommand>
{
    private readonly IDocumentDbContext _db;

    public ModerateRatingHandler(IDocumentDbContext db) => _db = db;

    public async Task Handle(ModerateRatingCommand cmd, CancellationToken ct)
    {
        var rating = await _db.Ratings.FindAsync(new object[] { cmd.RatingId }, ct)
            ?? throw new Exception("Rating not found");

        rating.ModerationStatus = cmd.Status switch
        {
            "approved" => ReviewModerationStatus.Approved,
            "rejected" => ReviewModerationStatus.Rejected,
            _ => throw new Exception("Invalid moderation status")
        };

        rating.ModeratedById = cmd.ModeratedById;
        rating.ModeratedAt = DateTime.UtcNow;
        rating.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }
}