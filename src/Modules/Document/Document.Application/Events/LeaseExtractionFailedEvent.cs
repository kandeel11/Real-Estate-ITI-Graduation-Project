using MediatR;

namespace Document.Application.Events;

public record LeaseExtractionFailedEvent(
    Guid LeaseId,
    string ErrorMessage) : INotification;