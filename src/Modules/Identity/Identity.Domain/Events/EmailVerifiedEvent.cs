using MediatR;

namespace Identity.Domain.Events;

public record EmailVerifiedEvent(Guid UserId) : INotification;
