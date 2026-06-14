// Identity.Domain/Events/OtpRequestedEvent.cs

using MediatR;

namespace Identity.Domain.Events;

public sealed record OtpRequestedEvent(
    Guid UserId,
    string Email,
    DateTime ExpiresAtUtc) : INotification;