using MediatR;

namespace RealEstate.Shared.Kernel.Shared.IntegrationEvents;

/// <summary>
/// Published by the AI module after it finishes analyzing an owner's ownership documents.
/// Handled by the Property module to flip the property's verification status.
/// </summary>
public record OwnershipVerificationCompletedEvent(
    Guid PropertyId,
    Guid RequestId,
    string Status,                  // "AiVerified" | "RequiresManualReview" | "Rejected"
    decimal ConfidenceScore,
    decimal LocationMatchScore,
    bool FakeDetectionFlag,
    string Recommendation,          // "AUTO_VERIFY" | "MANUAL_REVIEW" | "REJECT"
    string? AnalysisNotes,
    DateTime CompletedAt
) : INotification;

/// <summary>
/// Published by the AI module when ownership analysis throws (download/OCR/AI failure).
/// </summary>
public record OwnershipVerificationFailedEvent(
    Guid PropertyId,
    Guid RequestId,
    string Reason
) : INotification;
