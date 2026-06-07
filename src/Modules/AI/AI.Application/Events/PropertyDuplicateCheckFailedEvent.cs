// AI.Application/Events/PropertyDuplicateCheckFailedEvent.cs

using MediatR;

namespace AI.Application.Events;

public sealed record PropertyDuplicateCheckFailedEvent(
    Guid PropertyId,
    string Reason
) : INotification;