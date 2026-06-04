using MediatR;

namespace Property.Domain.Events;

public record PropertySubmittedForReviewEvent(Guid PropertyId, Guid OwnerId) : INotification;
