using MediatR;
using Property.Application.Interfaces;

namespace Property.Application.Features.Commands.SetPropertyGate;

public class SetPropertyGateHandler : IRequestHandler<SetPropertyGateCommand>
{
    private readonly IPropertyRepository _repository;

    public SetPropertyGateHandler(IPropertyRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(SetPropertyGateCommand request, CancellationToken cancellationToken)
    {
        var property = await _repository.GetByIdAsync(request.PropertyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Property {request.PropertyId} not found.");

        if (request.Gate == PropertyGateType.Ownership)
            property.SetOwnershipGate(request.Status, request.Note);
        else
            property.SetDuplicateGate(request.Status, request.Note);

        // Any PropertyActivatedEvent raised by EvaluateGates is dispatched by the interceptor on save.
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
