namespace Identity.Application.Queries.GetMaintenanceCompaniesByService;

public record MaintenanceCompanyDto(
    Guid Id,
    string CompanyName,
    string[] ServiceTypes,
    string Email,
    string Phone,
    string City,
    bool IsActive,
    decimal RatingAvg,
    int TotalRatings);