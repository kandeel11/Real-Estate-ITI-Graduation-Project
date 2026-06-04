// AI.Application/Events/LeaseExtractionCompletedEvent.cs
// ✅ Defined by Mohamed — Yasser subscribes to this

using AI.Application.DTOs;
using MediatR;

namespace AI.Application.Events;

public sealed record LeaseExtractionCompletedEvent(
    Guid LeaseId,
    ExtractedLeaseData ExtractedData   // contains all fields + confidence scores
) : INotification;