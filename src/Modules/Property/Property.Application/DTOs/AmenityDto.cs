namespace Property.Application.DTOs;

public class AmenityDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string NameAr { get; init; } = string.Empty;
    public string? IconUrl { get; init; }
    public string Category { get; init; } = string.Empty;
}
