// Properties.Infrastructure/Services/PropertyFilterService.cs
using AI.Application.DTOs;
using AI.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Property.Application.Interfaces;
using Property.Domain.Enums;

namespace Properties.Infrastructure.Services;

/// <summary>
/// Implements IPropertyFilterService from AI.Application.
/// Applies Claude-parsed filters to the properties database.
/// </summary>
public sealed class PropertyFilterService : IPropertyFilterService
{
    private readonly IPropertyRepository _db;
    private readonly IDocumentStorageService _storage;

    public PropertyFilterService(
        IPropertyRepository db,
        IDocumentStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<(IReadOnlyList<PropertySearchItem> Items, int Total)> SearchAsync(
        ParsedSearchFilters filters,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        // 1. استخدام GetQueryable بدل Where مباشرة
        var query = _db.GetQueryable()
            .Where(p => !p.IsDeleted && p.Status == Property.Domain.Enums.PropertyStatus.Active);

        // ── Apply filters dynamically ─────────────────────────────────────────

        if (!string.IsNullOrWhiteSpace(filters.PropertyType) &&
    Enum.TryParse<PropertyType>(filters.PropertyType, true, out var parsedPropertyType))
        {
            query = query.Where(p => p.PropertyType == parsedPropertyType);
        }


        if (!string.IsNullOrWhiteSpace(filters.City))
            query = query.Where(p => p.City.Contains(filters.City));

        if (!string.IsNullOrWhiteSpace(filters.District))
            query = query.Where(p => p.District.Contains(filters.District));

        if (filters.Bedrooms.HasValue)
            query = query.Where(p => p.Bedrooms == filters.Bedrooms.Value);

        if (filters.Bathrooms.HasValue)
            query = query.Where(p => p.Bathrooms == filters.Bathrooms.Value);

        if (filters.MinArea.HasValue)
            query = query.Where(p => p.AreaSqm >= filters.MinArea.Value);

        if (filters.MaxArea.HasValue)
            query = query.Where(p => p.AreaSqm <= filters.MaxArea.Value);

        if (filters.Floor.HasValue)
            query = query.Where(p => p.FloorNumber == filters.Floor.Value);

        if (filters.MinPrice.HasValue)
            query = query.Where(p => p.Price >= filters.MinPrice.Value);

        if (filters.MaxPrice.HasValue)
            query = query.Where(p => p.Price <= filters.MaxPrice.Value);

        // Amenities filter — property must have ALL requested amenities
        foreach (var amenity in filters.Amenities)
        {
            query = amenity switch
            {
                "furnished" => query.Where(p => p.IsFurnished),
                "pet_friendly" => query.Where(p => p.PetsAllowed),
                //"pool" => query.Where(p => p.HasPool)
                //"gym" => query.Where(p => p.HasGym),
                //"parking" => query.Where(p => p.HasParking),
                //"elevator" => query.Where(p => p.HasElevator),
                //"garden" => query.Where(p => p.HasGarden),
                //"balcony" => query.Where(p => p.HasBalcony),
                //"security" => query.Where(p => p.HasSecurity),
                //"central_ac" => query.Where(p => p.HasCentralAc),
                //"pet_friendly" => query.Where(p => p.IsPetFriendly),
                _ => query
            };
        }

        var total = await query.CountAsync(ct);

        var items = await query
             .OrderByDescending(p => p.CreatedAt)
             .Skip((page - 1) * pageSize)
             .Take(pageSize)
             .Select(p => new
             {
                 p.Id,
                 p.Title,
                 p.PropertyType,
                 p.City,
                 p.District,
                 p.AreaSqm,
                 p.Bedrooms,
                 p.Bathrooms,
                 p.Price,
                 p.Description,
                 MainImagePath = p.Media
                                  .Where(m => m.IsPrimary)
                                  .Select(m => m.MediaUrl)
                                  .FirstOrDefault()
             })
             .ToListAsync(ct);

        // Generate SAS URLs for main images
        var result = new List<PropertySearchItem>();
        foreach (var item in items)
        {
            string? imageUrl = null;
            if (!string.IsNullOrEmpty(item.MainImagePath))
            {
                imageUrl = await _storage.GetSecureUrlAsync(
                    item.MainImagePath, TimeSpan.FromHours(1), ct);
            }

            result.Add(new PropertySearchItem(
                PropertyId: item.Id,
                Title: item.Title,
                PropertyType: item.PropertyType.ToString(),
                City: item.City,
                District: item.District,
                AreaInMeters: (double)item.AreaSqm,
                Bedrooms: item.Bedrooms,
                Bathrooms: item.Bathrooms,
                Price: item.Price,
                ListingType: "For Rent",
                Description: item.Description,
                MainImageUrl: imageUrl,
                MatchScore: 1.0));
        }

        return (result, total);
    }

    public async Task<bool> PropertyMatchesFiltersAsync(
        Guid propertyId,
        ParsedSearchFilters filters,
        CancellationToken ct = default)
    {
        // 1. استخدام GetQueryable بدل GetAllAsync
        var query = _db.GetQueryable()
            .Where(p => p.Id == propertyId &&
                        p.IsDeleted == false &&
                        p.Status == PropertyStatus.Active);

        if (!string.IsNullOrWhiteSpace(filters.PropertyType) &&
    Enum.TryParse<PropertyType>(filters.PropertyType, true, out var parsedPropertyType))
        {
            query = query.Where(p => p.PropertyType == parsedPropertyType);
        }


        // 2. استخدام Contains بدل ILike
        if (!string.IsNullOrWhiteSpace(filters.City))
            query = query.Where(p => p.City.Contains(filters.City));

        if (!string.IsNullOrWhiteSpace(filters.District))
            query = query.Where(p => p.District.Contains(filters.District));

        if (filters.Bedrooms.HasValue)
            query = query.Where(p => p.Bedrooms == filters.Bedrooms.Value);

        if (filters.MaxPrice.HasValue)
            query = query.Where(p => p.Price <= filters.MaxPrice.Value);

        if (filters.MinPrice.HasValue)
            query = query.Where(p => p.Price >= filters.MinPrice.Value);

        // 3. التنفيذ الفعلي للـ Query بدل ما يرجع true دايماً
        return await query.AnyAsync(ct);
    }
}