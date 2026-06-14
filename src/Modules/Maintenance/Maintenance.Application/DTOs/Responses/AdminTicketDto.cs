namespace Maintenance.Application.DTOs.Responses;

public record AdminTicketDto(
    // Identity
    Guid Id,
    string? TicketNumber,

    // Property
    Guid PropertyId,
    string? PropertyAddress,

    // Tenant  (email + phone come from the ticket; Name is enriched by the controller)
    Guid TenantId,
    string? TenantName,
    string? TenantEmail,
    string? TenantPhone,

    // Owner  (Name is enriched by the controller)
    Guid OwnerId,
    string? OwnerName,

    // Ticket content
    string Title,
    string Description,
    string Priority,
    string Status,
    string? ResponsibleParty,
    string? ResolutionMethod,

    // AI evaluation
    AiEvaluationDto? AiEvaluation,

    // Vendor assignment  (VendorCompanyName is enriched by the controller)
    VendorAssignmentDto? VendorAssignment,
    string? VendorCompanyName,

    // Sub-collections
    List<AttachmentDto> Attachments,
    List<CommentDto> Comments,
    List<StatusHistoryItemDto> StatusHistory,

    // Payment
    PaymentDto? Payment,

    // Dates
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? ResolvedAt
);
