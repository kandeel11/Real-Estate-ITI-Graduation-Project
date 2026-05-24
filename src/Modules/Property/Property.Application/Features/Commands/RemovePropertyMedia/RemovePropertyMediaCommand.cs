using MediatR;

namespace Property.Application.Features.Commands.RemovePropertyMedia;

public record RemovePropertyMediaCommand(Guid PropertyId, Guid MediaId, Guid RequestingUserId) : IRequest;
