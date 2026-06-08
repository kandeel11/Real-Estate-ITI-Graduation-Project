using MediatR;

namespace Identity.Domain.Events;

public record UserRegisteredEvent(Guid UserId, string Role) : INotification;
