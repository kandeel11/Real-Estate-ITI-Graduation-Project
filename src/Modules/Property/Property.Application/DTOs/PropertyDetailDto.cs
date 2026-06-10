namespace Property.Application.DTOs;

public class PropertyDetailDto
{
    public Guid Id { get; init; }
    public Guid OwnerId { get; init; }
    public Guid? CompanyId { get; init; }
    public Guid? ListingBrokerId { get; init; }

    public string Title { get; init; } = string.Empty;
    public string TitleAr { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string DescriptionAr { get; init; } = string.Empty;

    public string PropertyType { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int Bedrooms { get; init; }
    public int Bathrooms { get; init; }
    public decimal AreaSqm { get; init; }
    public int FloorNumber { get; init; }
    public int TotalFloors { get; init; }
    public int YearBuilt { get; init; }
    public bool IsFurnished { get; init; }
    public bool PetsAllowed { get; init; }

    public string Address { get; init; } = string.Empty;
    public int? ApartmentNumber { get; init; }
    public string City { get; init; } = string.Empty;
    public string District { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string Governorate { get; init; } = string.Empty;

    public string ListingSource { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public bool IsFeatured { get; init; }
    public DateTime? FeaturedUntil { get; init; }

    public decimal? AiDuplicateConfidence { get; init; }
    public string VerificationStatus { get; init; } = string.Empty;
    public string? RejectionReason { get; init; }

    // Two-gate verification state
    public string OwnershipGate { get; init; } = string.Empty;
    public string DuplicateGate { get; init; } = string.Empty;

    public int ViewsCount { get; init; }
    public int InquiriesCount { get; init; }

    // Enriched after query — populated by PropertyEnrichmentService in Host
    public string? ListingDisplayName { get; set; }
    public string? ListingBrokerName { get; set; }

    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    public List<PropertyMediaDto> Media { get; init; } = [];
    public List<AmenityDto> Amenities { get; init; } = [];
}
