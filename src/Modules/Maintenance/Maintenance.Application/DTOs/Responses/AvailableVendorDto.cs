namespace Maintenance.Application.DTOs.Responses;

public record AvailableVendorDto(
    Guid CompanyId,
    string CompanyName,
    string City,
    double AverageRating,
    int ActiveTicketsCount,
    int CompletedTicketsCount,
    string? Description,
    string? LogoUrl);
