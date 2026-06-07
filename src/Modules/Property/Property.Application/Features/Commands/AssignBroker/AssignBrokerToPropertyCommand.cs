using MediatR;

namespace Property.Application.Features.Commands.AssignBroker;

public record AssignBrokerToPropertyCommand(
    Guid PropertyId,
    Guid RequestingUserId,
    Guid? RequestingCompanyId,
    Guid? BrokerId
) : IRequest;
