namespace Property.Application.DTOs;

public class PropertySummaryDto
{
    public Guid Id { get; init; }
    public Guid OwnerId { get; init; }
    public Guid? CompanyId { get; init; }
    public Guid? ListingBrokerId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string TitleAr { get; init; } = string.Empty;
    public string PropertyType { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int Bedrooms { get; init; }
    public int Bathrooms { get; init; }
    public decimal AreaSqm { get; init; }
    public bool IsFurnished { get; init; }
    public bool PetsAllowed { get; init; }
    public string City { get; init; } = string.Empty;
    public string District { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string VerificationStatus { get; init; } = string.Empty;
    public bool IsFeatured { get; init; }
    public string? PrimaryImageUrl { get; init; }
    public int ViewsCount { get; init; }
    public DateTime CreatedAt { get; init; }

    // Amenity ids for browse-page filtering (the summary stays light — names
    // are resolved client-side against the /amenities catalog).
    public List<Guid> AmenityIds { get; init; } = [];

    // Enriched after query — populated by PropertyEnrichmentService in Host
    public string? ListingDisplayName { get; set; }
    public string? ListingBrokerName { get; set; }
}
