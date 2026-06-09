using Document.Application.Interfaces;
using Document.Domain.Enums;
using MediatR;

namespace Document.Application.Commands.Rating.CreateRating;

public class CreateRatingHandler : IRequestHandler<CreateRatingCommand, Guid>
{
    private readonly IDocumentDbContext _db;

    public CreateRatingHandler(IDocumentDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateRatingCommand cmd, CancellationToken ct)
    {
        if (cmd.Rating < 1 || cmd.Rating > 5)
            throw new Exception("Rating must be between 1 and 5");

        var rating = new Domain.Entities.Rating
        {
            Id = Guid.NewGuid(),
            ReviewerId = cmd.ReviewerId,
            LeaseId = cmd.LeaseId,
            PropertyId = cmd.PropertyId,
            CompanyId = cmd.CompanyId,
            BrokerId = cmd.BrokerId,
            MaintenanceCompanyId = cmd.MaintenanceCompanyId,
            RatingValue = cmd.Rating,
            ReviewText = cmd.ReviewText,
            ModerationStatus = ReviewModerationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Ratings.Add(rating);
        await _db.SaveChangesAsync(ct);

        return rating.Id;
    }
}