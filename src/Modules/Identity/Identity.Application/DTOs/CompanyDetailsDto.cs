namespace Identity.Application.DTOs;

/// <summary>
/// Full company profile including the optional branding fields that
/// UpdateCompanyCommand accepts (logo/website/description). Supersedes
/// CompanyDto for dashboard use (CompanyDto is kept untouched).
/// </summary>
public record CompanyDetailsDto(
    Guid Id,
    Guid ManagerId,
    string Name,
    string CommercialRegister,
    string? LogoUrl,
    string? Website,
    string? Description,
    string City,
    string Address,
    string Phone,
    string Email,
    bool IsVerified,
    bool IsActive,
    decimal RatingAvg,
    int TotalRatings,
    DateTime CreatedAt);
