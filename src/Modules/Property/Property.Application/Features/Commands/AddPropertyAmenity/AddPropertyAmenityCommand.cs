using MediatR;

namespace Property.Application.Features.Commands.AddPropertyAmenity;

public record AddPropertyAmenityCommand(Guid PropertyId, Guid AmenityId, Guid RequestingUserId) : IRequest;
