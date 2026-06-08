using MediatR;

namespace Document.Application.Commands.Rating.ModerateRating;

public record ModerateRatingCommand(
    Guid RatingId,
    Guid ModeratedById,
    string Status) : IRequest;