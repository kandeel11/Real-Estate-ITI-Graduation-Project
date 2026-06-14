using MediatR;

namespace Property.Application.Features.Commands.SetPrimaryMedia;

public record SetPrimaryMediaCommand(Guid PropertyId, Guid MediaId, Guid RequestingUserId) : IRequest;
