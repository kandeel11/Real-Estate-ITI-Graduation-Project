using Microsoft.EntityFrameworkCore;
using Property.Application.Common;
using Property.Application.DTOs;
using Property.Application.Interfaces;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Persistence.Contexts;

namespace Property.Infrastructure.Persistence.Repositories;

public class PropertyRepository : IPropertyRepository
{
    private readonly PropertyDbContext _context;

    public PropertyRepository(PropertyDbContext context)
    {
        _context = context;
    }

    public async Task<Domain.Entities.Property?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Properties.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<PagedResult<PropertySummaryDto>> SearchAsync(SearchCriteria criteria, CancellationToken ct = default)
    {
        var query = _context.Properties
            .Where(p => p.Status == PropertyStatus.Active)
            .AsQueryable();

        if (criteria.City is not null)
            query = query.Where(p => p.City == criteria.City);

        if (criteria.District is not null)
            query = query.Where(p => p.District == criteria.District);

        if (criteria.PropertyType is not null && Enum.TryParse<PropertyType>(criteria.PropertyType, true, out var pt))
            query = query.Where(p => p.PropertyType == pt);

        if (criteria.MinPrice.HasValue)
            query = query.Where(p => p.Price >= criteria.MinPrice.Value);

        if (criteria.MaxPrice.HasValue)
            query = query.Where(p => p.Price <= criteria.MaxPrice.Value);

        if (criteria.MinBedrooms.HasValue)
            query = query.Where(p => p.Bedrooms >= criteria.MinBedrooms.Value);

        if (criteria.MaxBedrooms.HasValue)
            query = query.Where(p => p.Bedrooms <= criteria.MaxBedrooms.Value);

        if (criteria.Bedrooms.HasValue)
            query = query.Where(p => p.Bedrooms == criteria.Bedrooms.Value);

        if (criteria.IsFurnished.HasValue)
            query = query.Where(p => p.IsFurnished == criteria.IsFurnished.Value);

        if (criteria.PetsAllowed.HasValue)
            query = query.Where(p => p.PetsAllowed == criteria.PetsAllowed.Value);

        if (criteria.Latitude.HasValue && criteria.Longitude.HasValue && criteria.RadiusKm.HasValue)
        {
            var lat = (double)criteria.Latitude.Value;
            var lon = (double)criteria.Longitude.Value;
            var radiusKm = criteria.RadiusKm.Value;

            // Haversine approximation using bounding box pre-filter
            double latDelta = radiusKm / 111.0;
            double lonDelta = radiusKm / (111.0 * Math.Cos(lat * Math.PI / 180.0));

            var minLat = (decimal)(lat - latDelta);
            var maxLat = (decimal)(lat + latDelta);
            var minLon = (decimal)(lon - lonDelta);
            var maxLon = (decimal)(lon + lonDelta);

            query = query.Where(p =>
                p.Latitude >= minLat && p.Latitude <= maxLat &&
                p.Longitude >= minLon && p.Longitude <= maxLon);
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(p => p.IsFeatured)
            .ThenByDescending(p => p.CreatedAt)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .Select(p => new PropertySummaryDto
            {
                Id = p.Id,
                OwnerId = p.OwnerId,
                Title = p.Title,
                TitleAr = p.TitleAr,
                PropertyType = p.PropertyType.ToString(),
                Price = p.Price,
                Bedrooms = p.Bedrooms,
                Bathrooms = p.Bathrooms,
                AreaSqm = p.AreaSqm,
                IsFurnished = p.IsFurnished,
                PetsAllowed = p.PetsAllowed,
                City = p.City,
                District = p.District,
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                Status = p.Status.ToString(),
                IsFeatured = p.IsFeatured,
                PrimaryImageUrl = _context.PropertyMedia
                    .Where(m => m.PropertyId == p.Id && m.IsPrimary)
                    .Select(m => m.MediaUrl)
                    .FirstOrDefault(),
                ViewsCount = p.ViewsCount,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(ct);

        return new PagedResult<PropertySummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = criteria.Page,
            PageSize = criteria.PageSize
        };
    }

    public async Task<List<PropertySummaryDto>> GetAllAsync(CancellationToken ct = default)
        => await _context.Properties
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PropertySummaryDto
            {
                Id = p.Id,
                OwnerId = p.OwnerId,
                Title = p.Title,
                TitleAr = p.TitleAr,
                PropertyType = p.PropertyType.ToString(),
                Price = p.Price,
                Bedrooms = p.Bedrooms,
                Bathrooms = p.Bathrooms,
                AreaSqm = p.AreaSqm,
                IsFurnished = p.IsFurnished,
                PetsAllowed = p.PetsAllowed,
                City = p.City,
                District = p.District,
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                Status = p.Status.ToString(),
                IsFeatured = p.IsFeatured,
                PrimaryImageUrl = _context.PropertyMedia
                    .Where(m => m.PropertyId == p.Id && m.IsPrimary)
                    .Select(m => m.MediaUrl)
                    .FirstOrDefault(),
                ViewsCount = p.ViewsCount,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(ct);

    public async Task<List<PropertySummaryDto>> GetByOwnerAsync(Guid ownerId, CancellationToken ct = default)
        => await _context.Properties
            .Where(p => p.OwnerId == ownerId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PropertySummaryDto
            {
                Id = p.Id,
                OwnerId = p.OwnerId,
                Title = p.Title,
                TitleAr = p.TitleAr,
                PropertyType = p.PropertyType.ToString(),
                Price = p.Price,
                Bedrooms = p.Bedrooms,
                Bathrooms = p.Bathrooms,
                AreaSqm = p.AreaSqm,
                IsFurnished = p.IsFurnished,
                PetsAllowed = p.PetsAllowed,
                City = p.City,
                District = p.District,
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                Status = p.Status.ToString(),
                IsFeatured = p.IsFeatured,
                PrimaryImageUrl = _context.PropertyMedia
                    .Where(m => m.PropertyId == p.Id && m.IsPrimary)
                    .Select(m => m.MediaUrl)
                    .FirstOrDefault(),
                ViewsCount = p.ViewsCount,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(ct);

    public async Task<List<PropertySummaryDto>> GetPendingVerificationAsync(CancellationToken ct = default)
        => await _context.Properties
            .Where(p => p.Status == PropertyStatus.PendingReview)
            .OrderBy(p => p.CreatedAt)
            .Select(p => new PropertySummaryDto
            {
                Id = p.Id,
                OwnerId = p.OwnerId,
                Title = p.Title,
                TitleAr = p.TitleAr,
                PropertyType = p.PropertyType.ToString(),
                Price = p.Price,
                Bedrooms = p.Bedrooms,
                Bathrooms = p.Bathrooms,
                AreaSqm = p.AreaSqm,
                IsFurnished = p.IsFurnished,
                PetsAllowed = p.PetsAllowed,
                City = p.City,
                District = p.District,
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                Status = p.Status.ToString(),
                IsFeatured = p.IsFeatured,
                ViewsCount = p.ViewsCount,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(ct);

    public async Task<List<PropertySummaryDto>> GetFeaturedAsync(CancellationToken ct = default)
        => await _context.Properties
            .Where(p => p.Status == PropertyStatus.Active && p.IsFeatured &&
                        (p.FeaturedUntil == null || p.FeaturedUntil > DateTime.UtcNow))
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PropertySummaryDto
            {
                Id = p.Id,
                OwnerId = p.OwnerId,
                Title = p.Title,
                TitleAr = p.TitleAr,
                PropertyType = p.PropertyType.ToString(),
                Price = p.Price,
                Bedrooms = p.Bedrooms,
                Bathrooms = p.Bathrooms,
                AreaSqm = p.AreaSqm,
                IsFurnished = p.IsFurnished,
                PetsAllowed = p.PetsAllowed,
                City = p.City,
                District = p.District,
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                Status = p.Status.ToString(),
                IsFeatured = p.IsFeatured,
                PrimaryImageUrl = _context.PropertyMedia
                    .Where(m => m.PropertyId == p.Id && m.IsPrimary)
                    .Select(m => m.MediaUrl)
                    .FirstOrDefault(),
                ViewsCount = p.ViewsCount,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(ct);

    public async Task AddAsync(Domain.Entities.Property property, CancellationToken ct = default)
        => await _context.Properties.AddAsync(property, ct);

    public async Task<List<PropertyMediaDto>> GetMediaAsync(Guid propertyId, CancellationToken ct = default)
        => await _context.PropertyMedia
            .Where(m => m.PropertyId == propertyId)
            .OrderBy(m => m.SortOrder)
            .Select(m => new PropertyMediaDto
            {
                Id = m.Id,
                MediaUrl = m.MediaUrl,
                MediaType = m.MediaType.ToString(),
                IsPrimary = m.IsPrimary,
                SortOrder = m.SortOrder,
                FileSizeKb = m.FileSizeKb,
                UploadedAt = m.UploadedAt
            })
            .ToListAsync(ct);

    public async Task<PropertyMedia?> GetMediaByIdAsync(Guid mediaId, CancellationToken ct = default)
        => await _context.PropertyMedia.FirstOrDefaultAsync(m => m.Id == mediaId, ct);

    public async Task AddMediaAsync(PropertyMedia media, CancellationToken ct = default)
        => await _context.PropertyMedia.AddAsync(media, ct);

    public Task RemoveMediaAsync(PropertyMedia media, CancellationToken ct = default)
    {
        _context.PropertyMedia.Remove(media);
        return Task.CompletedTask;
    }

    public async Task<List<AmenityDto>> GetPropertyAmenitiesAsync(Guid propertyId, CancellationToken ct = default)
        => await _context.PropertyAmenities
            .Where(pa => pa.PropertyId == propertyId)
            .Join(_context.Amenities, pa => pa.AmenityId, a => a.Id, (pa, a) => new AmenityDto
            {
                Id = a.Id,
                Name = a.Name,
                NameAr = a.NameAr,
                IconUrl = a.IconUrl,
                Category = a.Category
            })
            .ToListAsync(ct);

    public async Task<List<AmenityDto>> GetAllAmenitiesAsync(CancellationToken ct = default)
        => await _context.Amenities
            .OrderBy(a => a.Category).ThenBy(a => a.Name)
            .Select(a => new AmenityDto
            {
                Id = a.Id,
                Name = a.Name,
                NameAr = a.NameAr,
                IconUrl = a.IconUrl,
                Category = a.Category
            })
            .ToListAsync(ct);

    public async Task<Amenity?> GetAmenityByIdAsync(Guid amenityId, CancellationToken ct = default)
        => await _context.Amenities.FirstOrDefaultAsync(a => a.Id == amenityId, ct);

    public async Task<bool> PropertyAmenityExistsAsync(Guid propertyId, Guid amenityId, CancellationToken ct = default)
        => await _context.PropertyAmenities.AnyAsync(pa => pa.PropertyId == propertyId && pa.AmenityId == amenityId, ct);

    public async Task AddPropertyAmenityAsync(PropertyAmenity propertyAmenity, CancellationToken ct = default)
        => await _context.PropertyAmenities.AddAsync(propertyAmenity, ct);

    public async Task<PropertyAmenity?> GetPropertyAmenityAsync(Guid propertyId, Guid amenityId, CancellationToken ct = default)
        => await _context.PropertyAmenities.FirstOrDefaultAsync(pa => pa.PropertyId == propertyId && pa.AmenityId == amenityId, ct);

    public Task RemovePropertyAmenityAsync(PropertyAmenity propertyAmenity, CancellationToken ct = default)
    {
        _context.PropertyAmenities.Remove(propertyAmenity);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
