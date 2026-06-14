using MediatR;

namespace Property.Application.Features.Commands.SetFeatured;

public record SetFeaturedCommand(
    Guid PropertyId,
    Guid RequestingUserId,
    Guid? CompanyId,
    bool IsFeatured,
    DateTime? FeaturedUntil
) : IRequest;
