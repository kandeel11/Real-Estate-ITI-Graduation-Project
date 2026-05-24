using MediatR;

namespace Property.Application.Features.Commands.SubmitPropertyForReview;

public record SubmitPropertyForReviewCommand(Guid PropertyId, Guid RequestingUserId) : IRequest;
