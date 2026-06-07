using MediatR;
using Property.Application.Interfaces;

namespace Property.Application.Features.Commands.MarkAsRented;

public class MarkAsRentedHandler : IRequestHandler<MarkAsRentedCommand>
{
    private readonly IPropertyRepository _repository;
    private readonly IPublisher _publisher;

    public MarkAsRentedHandler(IPropertyRepository repository, IPublisher publisher)
    {
        _repository = repository;
        _publisher = publisher;
    }

    public async Task Handle(MarkAsRentedCommand request, CancellationToken cancellationToken)
    {
        var property = await _repository.GetByIdAsync(request.PropertyId, cancellationToken)
            ?? throw new InvalidOperationException("Property not found.");

        bool isOwner = property.OwnerId == request.RequestingUserId;
        bool isSameCompany = property.CompanyId.HasValue; // company manager or broker handled at controller level

        if (!isOwner && !isSameCompany)
            throw new UnauthorizedAccessException("Only the property owner or their company can mark it as rented.");

        property.MarkAsRented();
        await _repository.SaveChangesAsync(cancellationToken);

        foreach (var domainEvent in property.DomainEvents)
            await _publisher.Publish(domainEvent, cancellationToken);
        property.ClearDomainEvents();
    }
}
