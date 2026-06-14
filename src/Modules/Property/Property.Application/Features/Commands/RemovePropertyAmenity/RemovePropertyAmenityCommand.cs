using MediatR;

namespace Property.Application.Features.Commands.RemovePropertyAmenity;

public record RemovePropertyAmenityCommand(Guid PropertyId, Guid AmenityId, Guid RequestingUserId) : IRequest;
