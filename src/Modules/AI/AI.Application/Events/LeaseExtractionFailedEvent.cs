// AI.Application/Events/LeaseExtractionFailedEvent.cs

using MediatR;

namespace AI.Application.Events;

public sealed record LeaseExtractionFailedEvent(
    Guid LeaseId,
    string Reason
) : INotification;