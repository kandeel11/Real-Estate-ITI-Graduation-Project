using MediatR;

namespace Property.Application.Features.Commands.RequestAiDescription;

public record PropertyAiDescriptionRequestCommand(
    Guid PropertyId,
    Guid RequestingUserId
) : IRequest;
