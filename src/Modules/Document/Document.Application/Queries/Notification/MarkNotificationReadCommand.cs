using Document.Application.Interfaces;
using MediatR;

namespace Document.Application.Commands.Notification;

public record MarkNotificationReadCommand(Guid NotificationId) : IRequest;

public class MarkNotificationReadHandler(INotificationRepository repo)
    : IRequestHandler<MarkNotificationReadCommand>
{
    public async Task Handle(MarkNotificationReadCommand cmd, CancellationToken ct)
        => await repo.MarkReadAsync(cmd.NotificationId, ct);
}