using MediatR;
using Property.Application.Interfaces;

namespace Property.Application.Features.Commands.RemovePropertyAmenity;

public class RemovePropertyAmenityHandler : IRequestHandler<RemovePropertyAmenityCommand>
{
    private readonly IPropertyRepository _repository;

    public RemovePropertyAmenityHandler(IPropertyRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(RemovePropertyAmenityCommand request, CancellationToken cancellationToken)
    {
        var property = await _repository.GetByIdAsync(request.PropertyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Property {request.PropertyId} not found.");

        if (property.OwnerId != request.RequestingUserId)
            throw new UnauthorizedAccessException("Only the property owner can remove amenities.");

        var propertyAmenity = await _repository.GetPropertyAmenityAsync(request.PropertyId, request.AmenityId, cancellationToken)
            ?? throw new KeyNotFoundException($"Amenity {request.AmenityId} is not linked to property {request.PropertyId}.");

        await _repository.RemovePropertyAmenityAsync(propertyAmenity, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
