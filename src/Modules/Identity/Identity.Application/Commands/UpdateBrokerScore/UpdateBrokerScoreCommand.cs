using MediatR;

namespace Identity.Application.Commands.UpdateBrokerScore;

public record UpdateBrokerScoreCommand(Guid BrokerId, decimal Score) : IRequest;