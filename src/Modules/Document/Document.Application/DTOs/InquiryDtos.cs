namespace Document.Application.DTOs;

public record CreateInquiryRequest(
    Guid PropertyId,
    Guid TenantId,
    Guid OwnerId,
    Guid? CompanyId,
    string Message
);

public record AcceptInquiryRequest(
    Guid AcceptedById
);

public record InquiryResponse(
    Guid Id,
    Guid PropertyId,
    Guid TenantId,
    Guid OwnerId,
    Guid? CompanyId,
    string Message,
    string Status,
    Guid? AcceptedById,
    DateTime? AcceptedAt,
    DateTime? ClosedAt,
    DateTime CreatedAt
)
{
    public Guid? LeadId { get; init; }
};

public record NotificationResponse(
    Guid Id,
    Guid UserId,
    string Type,
    string Title,
    string Body,
    string? RelatedEntityType,
    Guid? RelatedEntityId,
    bool IsRead,
    DateTime? ReadAt,
    DateTime CreatedAt
);