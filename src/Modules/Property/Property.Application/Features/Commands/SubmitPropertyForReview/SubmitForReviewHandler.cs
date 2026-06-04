using MediatR;
using Property.Application.Interfaces;

namespace Property.Application.Features.Commands.SubmitPropertyForReview;

public class SubmitForReviewHandler : IRequestHandler<SubmitPropertyForReviewCommand>
{
    private readonly IPropertyRepository _repository;
    private readonly IPublisher _publisher;

    public SubmitForReviewHandler(IPropertyRepository repository, IPublisher publisher)
    {
        _repository = repository;
        _publisher = publisher;
    }

    public async Task Handle(SubmitPropertyForReviewCommand request, CancellationToken cancellationToken)
    {
        var property = await _repository.GetByIdAsync(request.PropertyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Property {request.PropertyId} not found.");

        if (property.OwnerId != request.RequestingUserId)
            throw new UnauthorizedAccessException("Only the property owner can submit for review.");

        property.SubmitForReview();

        await _repository.SaveChangesAsync(cancellationToken);

        foreach (var domainEvent in property.DomainEvents)
            await _publisher.Publish(domainEvent, cancellationToken);

        property.ClearDomainEvents();
    }
}
