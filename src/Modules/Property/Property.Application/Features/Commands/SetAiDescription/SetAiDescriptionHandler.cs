using MediatR;
using Property.Application.Interfaces;

namespace Property.Application.Features.Commands.SetAiDescription;

public class SetAiDescriptionHandler : IRequestHandler<SetAiDescriptionCommand>
{
    private readonly IPropertyRepository _repository;

    public SetAiDescriptionHandler(IPropertyRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(SetAiDescriptionCommand request, CancellationToken cancellationToken)
    {
        var property = await _repository.GetByIdAsync(request.PropertyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Property {request.PropertyId} not found.");

        property.SetAiDescription(request.DescriptionEn, request.DescriptionAr);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
