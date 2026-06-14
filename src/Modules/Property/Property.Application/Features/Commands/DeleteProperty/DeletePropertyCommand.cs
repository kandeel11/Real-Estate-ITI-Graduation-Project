using MediatR;

namespace Property.Application.Features.Commands.DeleteProperty;

public record DeletePropertyCommand(Guid PropertyId, Guid RequestingUserId) : IRequest;
