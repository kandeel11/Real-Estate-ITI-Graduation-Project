using Document.Application.Interfaces;
using Document.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Document.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly DocumentDbContext _db;
    public NotificationRepository(DocumentDbContext db) => _db = db;

    public async Task<List<Domain.Entities.Notification>> GetByUserIdAsync(Guid userId, bool unreadOnly, CancellationToken ct)
        => await _db.Notifications
            .Where(x => x.UserId == userId && (!unreadOnly || !x.IsRead))
            .OrderByDescending(x => x.CreatedAt).ToListAsync(ct);

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct)
        => await _db.Notifications.CountAsync(x => x.UserId == userId && !x.IsRead, ct);

    public async Task AddAsync(Domain.Entities.Notification notification, CancellationToken ct)
    {
        await _db.Notifications.AddAsync(notification, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkReadAsync(Guid notificationId, CancellationToken ct)
    {
        var n = await _db.Notifications.FindAsync([notificationId], ct);
        if (n is not null) { n.IsRead = true; n.ReadAt = DateTime.UtcNow; await _db.SaveChangesAsync(ct); }
    }

    public async Task MarkAllReadAsync(Guid userId, CancellationToken ct)
        => await _db.Notifications.Where(x => x.UserId == userId && !x.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRead, true).SetProperty(x => x.ReadAt, DateTime.UtcNow), ct);
}