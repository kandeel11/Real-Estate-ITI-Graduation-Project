using MediatR;
using Property.Application.Interfaces;
using Property.Domain.Enums;

namespace Property.Application.Features.Commands.SyndicateProperty;

public class SyndicatePropertyHandler : IRequestHandler<SyndicatePropertyCommand>
{
    private readonly IPropertyRepository _repository;
    private readonly IPropertySyndicationService _syndication;

    public SyndicatePropertyHandler(IPropertyRepository repository, IPropertySyndicationService syndication)
    {
        _repository = repository;
        _syndication = syndication;
    }

    public async Task Handle(SyndicatePropertyCommand request, CancellationToken cancellationToken)
    {
        var property = await _repository.GetByIdAsync(request.PropertyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Property {request.PropertyId} not found.");

        if (property.OwnerId != request.RequestingUserId && property.CompanyId != request.RequestingUserId)
            throw new UnauthorizedAccessException("Not authorized to syndicate this property.");

        if (property.Status != PropertyStatus.Active)
            throw new InvalidOperationException("Only active properties can be syndicated.");

        await _syndication.SyndicateAsync(request.PropertyId, cancellationToken);
    }
}
