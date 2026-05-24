using MediatR;
using Property.Application.Interfaces;

namespace Property.Application.Features.Commands.CreateProperty;

public class CreatePropertyHandler : IRequestHandler<CreatePropertyCommand, Guid>
{
    private readonly IPropertyRepository _repository;

    public CreatePropertyHandler(IPropertyRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(CreatePropertyCommand request, CancellationToken cancellationToken)
    {
        var property = Domain.Entities.Property.Create(
            request.OwnerId,
            request.CompanyId,
            request.ListingBrokerId,
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
            request.ListingSource
        );

        await _repository.AddAsync(property, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return property.Id;
    }
}
