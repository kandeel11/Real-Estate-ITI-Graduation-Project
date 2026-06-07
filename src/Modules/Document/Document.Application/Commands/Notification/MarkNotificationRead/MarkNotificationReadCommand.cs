using MediatR;

namespace Document.Application.Commands.Notification.MarkNotificationRead;

public record MarkNotificationReadCommand(Guid NotificationId) : IRequest;