using MediatR;
using Property.Application.Interfaces;
using RealEstate.Shared.Kernel.Shared.Contracts;

namespace Property.Application.Features.Commands.CreateProperty;

public class CreatePropertyHandler : IRequestHandler<CreatePropertyCommand, Guid>
{
    private readonly IPropertyRepository _repository;
    private readonly ISubscriptionLimitChecker _limitChecker;

    public CreatePropertyHandler(IPropertyRepository repository, ISubscriptionLimitChecker limitChecker)
    {
        _repository = repository;
        _limitChecker = limitChecker;
    }

    public async Task<Guid> Handle(CreatePropertyCommand request, CancellationToken cancellationToken)
    {
        var (allowed, reason) = await _limitChecker.CheckPropertyLimitAsync(request.OwnerId, request.CompanyId, cancellationToken);
        if (!allowed)
            throw new InvalidOperationException(reason);

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

        await _limitChecker.IncrementPropertyUsageAsync(request.OwnerId, request.CompanyId, cancellationToken);

        return property.Id;
    }
}
