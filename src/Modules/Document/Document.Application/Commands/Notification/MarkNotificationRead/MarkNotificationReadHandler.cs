using Document.Application.Interfaces;
using MediatR;

namespace Document.Application.Commands.Notification.MarkNotificationRead;

public class MarkNotificationReadHandler : IRequestHandler<MarkNotificationReadCommand>
{
    private readonly IDocumentDbContext _db;

    public MarkNotificationReadHandler(IDocumentDbContext db) => _db = db;

    public async Task Handle(MarkNotificationReadCommand cmd, CancellationToken ct)
    {
        var notification = await _db.Notifications.FindAsync(new object[] { cmd.NotificationId }, ct)
            ?? throw new Exception("Notification not found");

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }
}