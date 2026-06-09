using MediatR;
using Property.Application.Interfaces;
using Property.Domain.Events;
using RealEstate.Shared.Kernel.Shared.Contracts;

namespace Property.Application.Features.Commands.CreateProperty;

public class CreatePropertyHandler : IRequestHandler<CreatePropertyCommand, Guid>
{
    private readonly IPropertyRepository _repository;
    private readonly ISubscriptionLimitChecker _limitChecker;
    private readonly IPublisher _publisher;

    public CreatePropertyHandler(IPropertyRepository repository, ISubscriptionLimitChecker limitChecker, IPublisher publisher)
    {
        _repository = repository;
        _limitChecker = limitChecker;
        _publisher = publisher;
    }

    public async Task<Guid> Handle(CreatePropertyCommand request, CancellationToken cancellationToken)
    {
        var (allowed, reason) = await _limitChecker.CheckPropertyLimitAsync(request.OwnerId, request.CompanyId, cancellationToken);
        if (!allowed)
            throw new InvalidOperationException(reason);

        if (await _repository.ExistsByOwnerAndTitleAsync(request.OwnerId, request.Title, cancellationToken))
            throw new InvalidOperationException($"You already have a property listed with the title '{request.Title}'.");

        if (request.ApartmentNumber is not null &&
            await _repository.ExistsByOwnerAndAddressAsync(request.OwnerId, request.Address, request.ApartmentNumber.Value, cancellationToken))
            throw new InvalidOperationException($"You already have apartment '{request.ApartmentNumber}' at '{request.Address}' listed.");

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
      request.FloorNumber ?? 0,                // Command has int?, Entity expects int
      request.TotalFloors ?? 0,                // Command has int?, Entity expects int
      request.YearBuilt ?? 0,                  // Command has int?, Entity expects int
      request.IsFurnished,
      request.PetsAllowed,
      request.Latitude,
      request.Longitude,
      request.Address,
      request.ApartmentNumber,                 // Matches (int?)
      request.Country,
      request.Governorate,
      request.ownerShipStatus,
      request.OwnerFullName ?? string.Empty,   // Command has string?, Entity expects string
      request.NationalIdBlobPath ?? string.Empty, // Command has string?, Entity expects string
      request.OwnershipProofBlobPath ?? string.Empty, // Command has string?, Entity expects string
      request.City,
      request.District ?? string.Empty,        // Command has string?, Entity expects string
      request.ListingSource
  );

        await _repository.AddAsync(property, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        await _limitChecker.IncrementPropertyUsageAsync(request.OwnerId, request.CompanyId, cancellationToken);

        await _publisher.Publish(new PropertyCreatedEvent(
          PropertyId: property.Id,
          OwnerUserId: property.OwnerId,
          OwnerName: request.OwnerFullName,
          OwnerNationalIdBlobPath: request.NationalIdBlobPath,
          OwnershipProofBlobPath: request.OwnershipProofBlobPath,
          Country: request.Country,
          Governorate: request.Governorate,
          City: request.City,
          District: request.District,
          AddressLine: request.Address
      ), cancellationToken);

        return property.Id;
    }
}
