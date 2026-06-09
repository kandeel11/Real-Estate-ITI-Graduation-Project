using Document.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Commands.Message.MarkMessagesRead;

public class MarkMessagesReadHandler : IRequestHandler<MarkMessagesReadCommand>
{
    private readonly IDocumentDbContext _db;

    public MarkMessagesReadHandler(IDocumentDbContext db) => _db = db;

    public async Task Handle(MarkMessagesReadCommand cmd, CancellationToken ct)
    {
        var unreadMessages = await _db.Messages
            .Where(m => m.ConversationId == cmd.ConversationId
                     && m.SenderId != cmd.ReaderId
                     && !m.IsRead
                     && m.DeletedAt == null)
            .ToListAsync(ct);

        foreach (var msg in unreadMessages)
        {
            msg.IsRead = true;
            msg.ReadAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }
}