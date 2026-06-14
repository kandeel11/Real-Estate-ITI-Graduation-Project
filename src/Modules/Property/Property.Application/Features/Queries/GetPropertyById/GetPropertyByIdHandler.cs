using MediatR;
using Property.Application.DTOs;
using Property.Application.Interfaces;

namespace Property.Application.Features.Queries.GetPropertyById;

public class GetPropertyByIdHandler : IRequestHandler<GetPropertyByIdQuery, PropertyDetailDto?>
{
    private readonly IPropertyRepository _repository;

    public GetPropertyByIdHandler(IPropertyRepository repository)
    {
        _repository = repository;
    }

    public async Task<PropertyDetailDto?> Handle(GetPropertyByIdQuery request, CancellationToken cancellationToken)
    {
        var property = await _repository.GetByIdAsync(request.PropertyId, cancellationToken);
        if (property is null) return null;

        var media = await _repository.GetMediaAsync(request.PropertyId, cancellationToken);
        var amenities = await _repository.GetPropertyAmenitiesAsync(request.PropertyId, cancellationToken);

        return new PropertyDetailDto
        {
            Id = property.Id,
            OwnerId = property.OwnerId,
            CompanyId = property.CompanyId,
            ListingBrokerId = property.ListingBrokerId,
            Title = property.Title,
            TitleAr = property.TitleAr,
            Description = property.Description,
            DescriptionAr = property.DescriptionAr,
            PropertyType = property.PropertyType.ToString(),
            Price = property.Price,
            Bedrooms = property.Bedrooms,
            Bathrooms = property.Bathrooms,
            AreaSqm = property.AreaSqm,
            FloorNumber = property.FloorNumber,
            TotalFloors = property.TotalFloors,
            YearBuilt = property.YearBuilt,
            IsFurnished = property.IsFurnished,
            PetsAllowed = property.PetsAllowed,
            Address = property.Address,
            ApartmentNumber = property.ApartmentNumber,
            City = property.City,
            District = property.District,
            Country = property.Country,
            Governorate = property.Governorate,
            ListingSource = property.ListingSource.ToString(),
            Status = property.Status.ToString(),
            IsFeatured = property.IsFeatured,
            FeaturedUntil = property.FeaturedUntil,
            AiDuplicateConfidence = property.AiDuplicateConfidence,
            VerificationStatus = property.VerificationStatus.ToString(),
            RejectionReason = property.RejectionReason,
            OwnershipGate = property.OwnershipGate.ToString(),
            DuplicateGate = property.DuplicateGate.ToString(),
            ViewsCount = property.ViewsCount,
            InquiriesCount = property.InquiriesCount,
            CreatedAt = property.CreatedAt,
            UpdatedAt = property.UpdatedAt,
            Media = media,
            Amenities = amenities
        };
    }
}
