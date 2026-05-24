using MediatR;
using Property.Application.Interfaces;

namespace Property.Application.Features.Commands.SetVerificationStatus;

public class SetVerificationStatusHandler : IRequestHandler<SetVerificationStatusCommand>
{
    private readonly IPropertyRepository _repository;
    private readonly IPublisher _publisher;

    public SetVerificationStatusHandler(IPropertyRepository repository, IPublisher publisher)
    {
        _repository = repository;
        _publisher = publisher;
    }

    public async Task Handle(SetVerificationStatusCommand request, CancellationToken cancellationToken)
    {
        var property = await _repository.GetByIdAsync(request.PropertyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Property {request.PropertyId} not found.");

        property.SetVerificationStatus(request.VerificationStatus, request.RejectionReason);

        await _repository.SaveChangesAsync(cancellationToken);

        foreach (var domainEvent in property.DomainEvents)
            await _publisher.Publish(domainEvent, cancellationToken);

        property.ClearDomainEvents();
    }
}
