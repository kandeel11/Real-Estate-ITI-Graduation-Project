using Identity.Application.Interfaces;
using MediatR;

namespace Identity.Application.Commands.UpdateBrokerScore;

public class UpdateBrokerScoreHandler : IRequestHandler<UpdateBrokerScoreCommand>
{
    private readonly IBrokerRepository _repo;

    public UpdateBrokerScoreHandler(IBrokerRepository repo) => _repo = repo;

    public async Task Handle(UpdateBrokerScoreCommand request, CancellationToken cancellationToken)
    {
        if (request.Score < 0 || request.Score > 100)
            throw new InvalidOperationException("Score must be between 0 and 100.");

        var broker = await _repo.GetByIdAsync(request.BrokerId, cancellationToken)
            ?? throw new InvalidOperationException("Broker not found.");

        broker.UpdateAiPerformanceScore(request.Score);
        await _repo.SaveChangesAsync(cancellationToken);
    }
}