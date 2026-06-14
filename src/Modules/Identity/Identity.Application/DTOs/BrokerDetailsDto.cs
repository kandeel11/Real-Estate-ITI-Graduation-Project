namespace Identity.Application.DTOs;

/// <summary>
/// Broker enriched with the linked user's identity and the performance
/// counters already tracked on the Broker entity. Supersedes BrokerDto for
/// dashboard use (BrokerDto is kept untouched for existing consumers).
/// </summary>
public record BrokerDetailsDto(
    Guid Id,
    Guid UserId,
    Guid? CompanyId,
    string FullName,
    string Email,
    string Phone,
    string? ProfileImageUrl,
    string LicenseNumber,
    bool IsActive,
    decimal AiPerformanceScore,
    int ClosedDealsCount,
    int AvgResponseTimeMinutes,
    decimal ConversionRate,
    decimal RatingAvg,
    int TotalRatings,
    DateTime CreatedAt);
