using MediatR;
using Property.Application.Interfaces;

namespace Property.Application.Features.Commands.UpdateProperty;

public class UpdatePropertyHandler : IRequestHandler<UpdatePropertyCommand>
{
    private readonly IPropertyRepository _repository;

    public UpdatePropertyHandler(IPropertyRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(UpdatePropertyCommand request, CancellationToken cancellationToken)
    {
        var property = await _repository.GetByIdAsync(request.PropertyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Property {request.PropertyId} not found.");

        if (property.OwnerId != request.RequestingUserId)
            throw new UnauthorizedAccessException("Only the property owner can update this listing.");

        property.Update(
            request.Title,
            request.TitleAr,
            request.Description,
            request.DescriptionAr,
            request.PropertyType,
            request.Price,
            request.Bedrooms,
            request.Bathrooms,
            request.AreaSqm,
            request.FloorNumber,
            request.TotalFloors,
            request.YearBuilt,
            request.IsFurnished,
            request.PetsAllowed,
            request.Latitude,
            request.Longitude,
            request.Address,
            request.City,
            request.District,
            request.ListingBrokerId
        );

        await _repository.SaveChangesAsync(cancellationToken);
    }
}
