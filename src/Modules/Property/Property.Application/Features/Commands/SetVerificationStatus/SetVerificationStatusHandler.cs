using MediatR;
using Property.Application.Interfaces;

namespace Property.Application.Features.Commands.SetVerificationStatus;

public class SetVerificationStatusHandler : IRequestHandler<SetVerificationStatusCommand>
{
    private readonly IPropertyRepository _repository;

    public SetVerificationStatusHandler(IPropertyRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(SetVerificationStatusCommand request, CancellationToken cancellationToken)
    {
        var property = await _repository.GetByIdAsync(request.PropertyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Property {request.PropertyId} not found.");

        property.SetVerificationStatus(request.VerificationStatus, request.RejectionReason);

        // PropertyActivatedEvent (when activated) is dispatched by DispatchDomainEventsInterceptor after save.
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
