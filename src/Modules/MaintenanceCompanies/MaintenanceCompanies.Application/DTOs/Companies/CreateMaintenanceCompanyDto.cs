using MaintenanceCompanies.Application.DTOs.Categories;

namespace MaintenanceCompanies.Application.DTOs.Companies;

// 1. تم مسح الـ InitialPassword من هنا لأن الـ Identity هو اللي بيتعامل معاه
public record CreateMaintenanceCompanyDto(
    string CompanyName,
    string Email,
    string Phone,
    string City,
    int ServiceRadiusKm,
    string? Description,
    string? LogoUrl,
    List<Guid> CategoryIds
);

public record UpdateMaintenanceCompanyDto(
    string CompanyName,
    string Phone,
    string City,
    int ServiceRadiusKm,
    string? Description,
    string? LogoUrl,
    List<Guid> CategoryIds
);

public record MaintenanceCompanyResponseDto(
    Guid Id,
    string CompanyName,
    string Email,
    string Phone,
    string City,
    int ServiceRadiusKm,
    string? LogoUrl,
    string? Description,
    bool IsActive,
    double AverageRating,
    int CompletedTicketsCount,
    int ActiveTicketsCount,
    DateTime CreatedAt,
    List<CategoryDto> Categories
);

public record MaintenanceCompanyListItemDto(
    Guid Id,
    string CompanyName,
    string City,
    bool IsActive,
    double AverageRating,
    int ActiveTicketsCount,
    List<string> CategoryNames
);

public record UpdateWorkingHoursDto(List<WorkingHourItemDto> Hours);

public record WorkingHourItemDto(DayOfWeek Day, TimeOnly? OpenTime, TimeOnly? CloseTime, bool IsClosed);

public record ReplyToReviewDto(string Reply);