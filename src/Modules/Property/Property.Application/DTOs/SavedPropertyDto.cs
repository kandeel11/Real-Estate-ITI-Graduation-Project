namespace Property.Application.DTOs;

/// <summary>
/// A saved (wishlisted) listing as shown in the tenant's "Saved" tab. Carries the
/// card fields plus when it was saved (newest-first ordering on the client).
/// </summary>
public class SavedPropertyDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string PropertyType { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int Bedrooms { get; init; }
    public int Bathrooms { get; init; }
    public decimal AreaSqm { get; init; }
    public string City { get; init; } = string.Empty;
    public string District { get; init; } = string.Empty;
    public bool IsFeatured { get; init; }
    public string? PrimaryImageUrl { get; init; }
    public DateTime SavedAt { get; init; }
}
