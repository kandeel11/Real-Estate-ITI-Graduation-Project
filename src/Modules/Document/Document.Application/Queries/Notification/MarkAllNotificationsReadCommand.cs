using Document.Application.Interfaces;
using MediatR;

namespace Document.Application.Commands.Notification;

public record MarkAllNotificationsReadCommand(Guid UserId) : IRequest;

public class MarkAllNotificationsReadHandler(INotificationRepository repo)
    : IRequestHandler<MarkAllNotificationsReadCommand>
{
    public async Task Handle(MarkAllNotificationsReadCommand cmd, CancellationToken ct)
        => await repo.MarkAllReadAsync(cmd.UserId, ct);
}