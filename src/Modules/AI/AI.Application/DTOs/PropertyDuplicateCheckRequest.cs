// AI.Application/DTOs/PropertyDuplicateCheckRequest.cs

namespace AI.Application.DTOs;

/// <summary>
/// All fields used for duplicate detection.
/// Text + Address + Type + Unit# are combined into the search vector.
/// </summary>
public sealed record PropertyDuplicateCheckRequest(
    Guid PropertyId,
    Guid OwnerId,

    // ── Text fields ───────────────────────────────
    string Title,
    string Description,         // raw owner notes

    // ── Address fields ────────────────────────────
    string City,
    string District,            // neighborhood / area
    string Street,              // street name
    string? BuildingName,        // "Rose Tower", "Nile View Compound"
    string? UnitNumber,          // apartment / unit number
    int? Floor,

    // ── Property type + specs ─────────────────────
    string PropertyType,        // Apartment, Villa, Office, Shop
    string ListingType,         // For Sale / For Rent
    double AreaInMeters,
    int? Bedrooms,
    int? Bathrooms,
    decimal Price
);