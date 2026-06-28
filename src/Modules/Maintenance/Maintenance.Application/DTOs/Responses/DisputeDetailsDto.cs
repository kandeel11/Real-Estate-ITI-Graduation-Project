namespace Maintenance.Application.DTOs.Responses;

/// <summary>
/// Full dispute context returned to the Admin for the dispute-resolution view.
/// Combines ticket data, AI decision, evidence images, dispute info, and lease contract.
/// </summary>
public record DisputeDetailsDto(
    // ── Ticket core ───────────────────────────────────────────────────────────
    Guid TicketId,
    string? TicketNumber,
    string Title,
    string Description,
    string Priority,
    string Status,
    string? PropertyAddress,
    Guid PropertyId,
    Guid TenantId,
    Guid OwnerId,
    DateTime CreatedAt,

    // ── Original AI decision ──────────────────────────────────────────────────
    AiEvaluationDto? AiEvaluation,

    // ── Evidence: image attachments from Azure Blob ───────────────────────────
    List<AttachmentDto> Images,

    // ── Dispute details ───────────────────────────────────────────────────────
    Guid? DisputedById,
    string? DisputedByRole,
    string? DisputeReason,
    DateTime? DisputeDate,

    // ── Admin resolution notes (null until resolved) ──────────────────────────
    string? AdminNotes,

    // ── Cross-module lease contract context ───────────────────────────────────
    LeaseContractSummaryDto? LeaseContract
);
