using Property.Application.Common;
using Property.Application.DTOs;
using Property.Domain.Entities;

namespace Property.Application.Interfaces;

public interface IPropertyRepository
{
    Task<Property.Domain.Entities.Property?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<PropertySummaryDto>> SearchAsync(SearchCriteria criteria, CancellationToken ct = default);
    Task<List<PropertySummaryDto>> GetAllAsync(CancellationToken ct = default);
    Task<List<PropertySummaryDto>> GetByOwnerAsync(Guid ownerId, CancellationToken ct = default);
    Task<List<PropertySummaryDto>> GetByCompanyAsync(Guid companyId, CancellationToken ct = default);
    Task<List<PropertySummaryDto>> GetPendingVerificationAsync(CancellationToken ct = default);
    Task<List<PropertySummaryDto>> GetFeaturedAsync(CancellationToken ct = default);
    Task<bool> ExistsByOwnerAndTitleAsync(Guid ownerId, string title, CancellationToken ct = default);
    Task<bool> ExistsByOwnerAndAddressAsync(Guid ownerId, string address, int apartmentNumber, CancellationToken ct = default);
    Task AddAsync(Property.Domain.Entities.Property property, CancellationToken ct = default);
    Task<List<PropertyMediaDto>> GetMediaAsync(Guid propertyId, CancellationToken ct = default);
    Task<PropertyMedia?> GetMediaByIdAsync(Guid mediaId, CancellationToken ct = default);
    Task AddMediaAsync(PropertyMedia media, CancellationToken ct = default);
    Task RemoveMediaAsync(PropertyMedia media, CancellationToken ct = default);
    Task<List<AmenityDto>> GetPropertyAmenitiesAsync(Guid propertyId, CancellationToken ct = default);
    Task<List<AmenityDto>> GetAllAmenitiesAsync(CancellationToken ct = default);
    Task<Amenity?> GetAmenityByIdAsync(Guid amenityId, CancellationToken ct = default);
    Task<bool> PropertyAmenityExistsAsync(Guid propertyId, Guid amenityId, CancellationToken ct = default);
    Task AddPropertyAmenityAsync(PropertyAmenity propertyAmenity, CancellationToken ct = default);
    Task<PropertyAmenity?> GetPropertyAmenityAsync(Guid propertyId, Guid amenityId, CancellationToken ct = default);
    Task RemovePropertyAmenityAsync(PropertyAmenity propertyAmenity, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    IQueryable<Domain.Entities.Property> GetQueryable();
}

public class SearchCriteria
{
    public string? City { get; init; }
    public string? District { get; init; }
    public string? PropertyType { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public int? MinBedrooms { get; init; }
    public int? MaxBedrooms { get; init; }
    public int? Bedrooms { get; init; }
    public bool? IsFurnished { get; init; }
    public bool? PetsAllowed { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
