using Document.Application.DTOs;
using Document.Application.Interfaces;
using Document.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Commands.Rating;

public record CreateRatingCommand(
    Guid ReviewerId, Guid? LeaseId, Guid? PropertyId,
    Guid? MaintenanceCompanyId, int RatingValue, string? ReviewText) : IRequest<RatingResponse>;

public class CreateRatingHandler(IDocumentDbContext db)
    : IRequestHandler<CreateRatingCommand, RatingResponse>
{
    public async Task<RatingResponse> Handle(CreateRatingCommand cmd, CancellationToken ct)
    {
        if (cmd.RatingValue < 1 || cmd.RatingValue > 5)
            throw new ArgumentException("Rating must be between 1 and 5.");

        if (cmd.LeaseId.HasValue)
        {
            var lease = await db.Leases.FirstOrDefaultAsync(
                l => l.Id == cmd.LeaseId.Value && l.TenantId == cmd.ReviewerId, ct);
            if (lease is null)
                throw new UnauthorizedAccessException("You can only rate a lease you are part of.");
        }

        if (cmd.PropertyId.HasValue)
        {
            var exists = await db.Ratings.AnyAsync(r =>
                r.ReviewerId == cmd.ReviewerId &&
                r.PropertyId == cmd.PropertyId &&
                r.DeletedAt == null, ct);
            if (exists)
                throw new InvalidOperationException("You have already submitted a rating for this property.");
        }

        var rating = new Domain.Entities.Rating
        {
            Id = Guid.NewGuid(),
            ReviewerId = cmd.ReviewerId,
            LeaseId = cmd.LeaseId,
            PropertyId = cmd.PropertyId,
            MaintenanceCompanyId = cmd.MaintenanceCompanyId,
            RatingValue = cmd.RatingValue,
            ReviewText = cmd.ReviewText,
            ModerationStatus = ReviewModerationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Ratings.Add(rating);
        await db.SaveChangesAsync(ct);
        return Map(rating);
    }

    public static RatingResponse Map(Domain.Entities.Rating r) => new(
        r.Id, r.ReviewerId, r.LeaseId, r.PropertyId, r.MaintenanceCompanyId,
        r.RatingValue, r.ReviewText, r.ModerationStatus.ToString(), r.CreatedAt, r.UpdatedAt);
}