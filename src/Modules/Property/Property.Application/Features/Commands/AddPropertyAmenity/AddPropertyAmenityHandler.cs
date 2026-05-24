using MediatR;
using Property.Application.Interfaces;
using Property.Domain.Entities;

namespace Property.Application.Features.Commands.AddPropertyAmenity;

public class AddPropertyAmenityHandler : IRequestHandler<AddPropertyAmenityCommand>
{
    private readonly IPropertyRepository _repository;

    public AddPropertyAmenityHandler(IPropertyRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(AddPropertyAmenityCommand request, CancellationToken cancellationToken)
    {
        var property = await _repository.GetByIdAsync(request.PropertyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Property {request.PropertyId} not found.");

        if (property.OwnerId != request.RequestingUserId)
            throw new UnauthorizedAccessException("Only the property owner can manage amenities.");

        _ = await _repository.GetAmenityByIdAsync(request.AmenityId, cancellationToken)
            ?? throw new KeyNotFoundException($"Amenity {request.AmenityId} not found.");

        bool exists = await _repository.PropertyAmenityExistsAsync(request.PropertyId, request.AmenityId, cancellationToken);
        if (exists)
            return;

        var propertyAmenity = PropertyAmenity.Create(request.PropertyId, request.AmenityId);
        await _repository.AddPropertyAmenityAsync(propertyAmenity, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
