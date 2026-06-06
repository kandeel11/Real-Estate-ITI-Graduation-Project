//// Properties.Infrastructure/Services/PropertyFilterService.cs

//using AI.Application.DTOs;
//using AI.Application.Interfaces;
//using Microsoft.EntityFrameworkCore;
//using Properties.Infrastructure.Persistence;
//using Property.Application.Interfaces;

//namespace Properties.Infrastructure.Services;

///// <summary>
///// Implements IPropertyFilterService from AI.Application.
///// Applies Claude-parsed filters to the properties database.
///// </summary>
//public sealed class PropertyFilterService : IPropertyFilterService
//{
//    private readonly IPropertyRepository _db;
//    private readonly IDocumentStorageService _storage;

//    public PropertyFilterService(
//        IPropertyRepository db,
//        IDocumentStorageService storage)
//    {
//        _db = db;
//        _storage = storage;
//    }

//    public async Task<(IReadOnlyList<PropertySearchItem> Items, int Total)> SearchAsync(
//        ParsedSearchFilters filters,
//        int page,
//        int pageSize,
//        CancellationToken ct = default)
//    {
//        var query = _db
//            .Where(p => p.IsActive && p.Status == "available")
//            .AsQueryable();

//        // ── Apply filters dynamically ─────────────────────────────────────────

//        if (!string.IsNullOrWhiteSpace(filters.PropertyType))
//            query = query.Where(p => p.PropertyType == filters.PropertyType);

//        if (!string.IsNullOrWhiteSpace(filters.ListingType))
//            query = query.Where(p => p.ListingType == filters.ListingType);

//        if (!string.IsNullOrWhiteSpace(filters.City))
//            query = query.Where(p =>
//                EF.Functions.ILike(p.City, $"%{filters.City}%"));

//        if (!string.IsNullOrWhiteSpace(filters.District))
//            query = query.Where(p =>
//                EF.Functions.ILike(p.District, $"%{filters.District}%"));

//        if (filters.Bedrooms.HasValue)
//            query = query.Where(p => p.Bedrooms == filters.Bedrooms.Value);

//        if (filters.Bathrooms.HasValue)
//            query = query.Where(p => p.Bathrooms == filters.Bathrooms.Value);

//        if (filters.MinArea.HasValue)
//            query = query.Where(p => p.AreaInMeters >= filters.MinArea.Value);

//        if (filters.MaxArea.HasValue)
//            query = query.Where(p => p.AreaInMeters <= filters.MaxArea.Value);

//        if (filters.Floor.HasValue)
//            query = query.Where(p => p.Floor == filters.Floor.Value);

//        if (filters.MinPrice.HasValue)
//            query = query.Where(p => p.Price >= filters.MinPrice.Value);

//        if (filters.MaxPrice.HasValue)
//            query = query.Where(p => p.Price <= filters.MaxPrice.Value);

//        // Amenities filter — property must have ALL requested amenities
//        foreach (var amenity in filters.Amenities)
//        {
//            query = amenity switch
//            {
//                "furnished" => query.Where(p => p.IsFurnished),
//                "pool" => query.Where(p => p.HasPool),
//                "gym" => query.Where(p => p.HasGym),
//                "parking" => query.Where(p => p.HasParking),
//                "elevator" => query.Where(p => p.HasElevator),
//                "garden" => query.Where(p => p.HasGarden),
//                "balcony" => query.Where(p => p.HasBalcony),
//                "security" => query.Where(p => p.HasSecurity),
//                "central_ac" => query.Where(p => p.HasCentralAc),
//                "pet_friendly" => query.Where(p => p.IsPetFriendly),
//                _ => query
//            };
//        }

//        var total = await query.CountAsync(ct);

//        var items = await query
//            .OrderByDescending(p => p.CreatedAt)
//            .Skip((page - 1) * pageSize)
//            .Take(pageSize)
//            .Select(p => new
//            {
//                p.Id,
//                p.Title,
//                p.PropertyType,
//                p.City,
//                p.District,
//                p.AreaInMeters,
//                p.Bedrooms,
//                p.Bathrooms,
//                p.Price,
//                p.ListingType,
//                p.Description,
//                p.MainImagePath
//            })
//            .ToListAsync(ct);

//        // Generate SAS URLs for main images
//        var result = new List<PropertySearchItem>();
//        foreach (var item in items)
//        {
//            string? imageUrl = null;
//            if (!string.IsNullOrEmpty(item.MainImagePath))
//            {
//                imageUrl = await _storage.GetSecureUrlAsync(
//                    item.MainImagePath, TimeSpan.FromHours(1), ct);
//            }

//            result.Add(new PropertySearchItem(
//                PropertyId: item.Id,
//                Title: item.Title,
//                PropertyType: item.PropertyType,
//                City: item.City,
//                District: item.District,
//                AreaInMeters: item.AreaInMeters,
//                Bedrooms: item.Bedrooms,
//                Bathrooms: item.Bathrooms,
//                Price: item.Price,
//                ListingType: item.ListingType,
//                Description: item.Description,
//                MainImageUrl: imageUrl,
//                MatchScore: 1.0));
//        }

//        return (result, total);
//    }

//    public async Task<bool> PropertyMatchesFiltersAsync(
//        Guid propertyId,
//        ParsedSearchFilters filters,
//        CancellationToken ct = default)
//    {
//        //var query = _db.Properties
//        //    .Where(p => p.Id == propertyId &&
//        //                p.IsActive &&
//        //                p.Status == "available");

//        //if (!string.IsNullOrWhiteSpace(filters.PropertyType))
//        //    query = query.Where(p => p.PropertyType == filters.PropertyType);

//        //if (!string.IsNullOrWhiteSpace(filters.ListingType))
//        //    query = query.Where(p => p.ListingType == filters.ListingType);

//        //if (!string.IsNullOrWhiteSpace(filters.City))
//        //    query = query.Where(p =>
//        //        EF.Functions.ILike(p.City, $"%{filters.City}%"));

//        //if (!string.IsNullOrWhiteSpace(filters.District))
//        //    query = query.Where(p =>
//        //        EF.Functions.ILike(p.District, $"%{filters.District}%"));

//        //if (filters.Bedrooms.HasValue)
//        //    query = query.Where(p => p.Bedrooms == filters.Bedrooms.Value);

//        //if (filters.MaxPrice.HasValue)
//        //    query = query.Where(p => p.Price <= filters.MaxPrice.Value);

//        //if (filters.MinPrice.HasValue)
//        //    query = query.Where(p => p.Price >= filters.MinPrice.Value);

//        return  true;
//    }
//}