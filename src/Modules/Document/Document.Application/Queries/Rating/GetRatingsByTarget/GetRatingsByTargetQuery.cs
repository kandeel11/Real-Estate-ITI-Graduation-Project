using MediatR;

namespace Document.Application.Queries.Rating.GetRatingsByTarget;

public record GetRatingsByTargetQuery(
    string TargetType,
    Guid TargetId) : IRequest<List<RatingDto>>;

public record RatingDto(
    Guid Id,
    Guid ReviewerId,
    int Rating,
    string? ReviewText,
    string ModerationStatus,
    DateTime CreatedAt);