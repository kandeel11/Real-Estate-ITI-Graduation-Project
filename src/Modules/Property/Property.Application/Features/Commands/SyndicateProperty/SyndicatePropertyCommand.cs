using MediatR;

namespace Property.Application.Features.Commands.SyndicateProperty;

public record SyndicatePropertyCommand(Guid PropertyId, Guid RequestingUserId) : IRequest;
