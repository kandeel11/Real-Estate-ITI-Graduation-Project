// AI.Application/DTOs/ParsedSearchFilters.cs

namespace AI.Application.DTOs;

/// <summary>
/// Structured filters extracted from natural language query by Claude.
/// All fields nullable — Claude only sets what it finds in the query.
/// </summary>
public sealed record ParsedSearchFilters(
    // Property type
    string? PropertyType,       // Apartment, Villa, Office, Shop, Studio
    string? ListingType,        // "For Rent" | "For Sale"

    // Location
    string? City,
    string? District,
    string? NearbyLandmark,     // "near university", "near metro"

    // Specs
    int? Bedrooms,
    int? Bathrooms,
    int? MinArea,
    int? MaxArea,
    int? Floor,

    // Price
    decimal? MinPrice,
    decimal? MaxPrice,

    // Amenities
    IReadOnlyList<string> Amenities,  // ["pool", "gym", "parking", "furnished", "pet_friendly"]

    // Metadata
    decimal Confidence,              // 0–1 — how confident Claude is in parsing
    string DetectedLanguage         // "ar" | "en" | "mixed"
);