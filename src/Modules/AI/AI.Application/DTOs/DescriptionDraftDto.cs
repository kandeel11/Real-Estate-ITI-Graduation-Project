namespace AI.Application.DTOs;

/// <summary>
/// The latest AI-generated description draft for a property — returned so the UI can
/// show it for review/editing before the owner accepts it.
/// </summary>
public record DescriptionDraftDto(
    Guid GenerationId,
    Guid PropertyId,
    string GeneratedTextEn,
    string GeneratedTextAr,
    bool IsAccepted,
    DateTime CreatedAt
);
