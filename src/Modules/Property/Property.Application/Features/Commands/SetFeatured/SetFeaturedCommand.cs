using MediatR;

namespace Property.Application.Features.Commands.SetFeatured;

public record SetFeaturedCommand(
    Guid PropertyId,
    Guid RequestingUserId,
    bool IsFeatured,
    DateTime? FeaturedUntil
) : IRequest;
