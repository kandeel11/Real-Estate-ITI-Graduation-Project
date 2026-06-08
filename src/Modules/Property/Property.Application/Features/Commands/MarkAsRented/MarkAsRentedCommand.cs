using MediatR;

namespace Property.Application.Features.Commands.MarkAsRented;

public record MarkAsRentedCommand(Guid PropertyId, Guid RequestingUserId) : IRequest;
