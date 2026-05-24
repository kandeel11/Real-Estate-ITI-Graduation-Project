using MediatR;
using Property.Application.Interfaces;

namespace Property.Application.Features.Commands.SetAiDuplicateScore;

public class SetAiDuplicateScoreHandler : IRequestHandler<SetAiDuplicateScoreCommand>
{
    private readonly IPropertyRepository _repository;

    public SetAiDuplicateScoreHandler(IPropertyRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(SetAiDuplicateScoreCommand request, CancellationToken cancellationToken)
    {
        var property = await _repository.GetByIdAsync(request.PropertyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Property {request.PropertyId} not found.");

        property.SetAiDuplicateConfidence(request.Confidence);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
