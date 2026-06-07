namespace Identity.Application.DTOs;

public record BrokerDto(
    Guid Id,
    Guid UserId,
    Guid? CompanyId,
    string LicenseNumber,
    bool IsActive,
    decimal AiPerformanceScore,
    decimal RatingAvg,
    int TotalRatings);
