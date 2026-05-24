using MediatR;

namespace Property.Domain.Events;

public record PropertyActivatedEvent(Guid PropertyId, Guid OwnerId) : INotification;
