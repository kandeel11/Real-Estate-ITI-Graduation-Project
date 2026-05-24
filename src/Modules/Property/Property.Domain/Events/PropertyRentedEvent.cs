using MediatR;

namespace Property.Domain.Events;

public record PropertyRentedEvent(Guid PropertyId, Guid OwnerId) : INotification;
