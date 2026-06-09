// AI.Application/Events/OwnershipVerificationEvents.cs
using MediatR;

namespace AI.Application.Events;

public sealed record OwnershipVerificationCompletedEvent(
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

public sealed record OwnershipVerificationFailedEvent(
    Guid PropertyId,
    Guid RequestId,
    string Reason) : INotification;