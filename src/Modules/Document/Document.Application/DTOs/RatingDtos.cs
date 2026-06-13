namespace Document.Application.DTOs;

public record CreateRatingRequest(
    Guid? LeaseId, Guid? PropertyId, Guid? MaintenanceCompanyId,
    int RatingValue, string? ReviewText);

public record UpdateRatingRequest(int RatingValue, string? ReviewText);

public record RatingResponse(
    Guid Id, Guid ReviewerId, Guid? LeaseId, Guid? PropertyId,
    Guid? MaintenanceCompanyId, int RatingValue, string? ReviewText,
    string ModerationStatus, DateTime CreatedAt, DateTime UpdatedAt);