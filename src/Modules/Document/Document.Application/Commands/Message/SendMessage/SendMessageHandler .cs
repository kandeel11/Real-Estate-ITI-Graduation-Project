using Document.Application.Interfaces;
using MediatR;

namespace Document.Application.Commands.Message.SendMessage;

public class SendMessageHandler : IRequestHandler<SendMessageCommand, Guid>
{
    private readonly IDocumentDbContext _db;

    public SendMessageHandler(IDocumentDbContext db) => _db = db;

    public async Task<Guid> Handle(SendMessageCommand cmd, CancellationToken ct)
    {
        var conversation = await _db.Conversations.FindAsync(new object[] { cmd.ConversationId }, ct)
            ?? throw new Exception("Conversation not found");

        var message = new Domain.Entities.Message
        {
            Id = Guid.NewGuid(),
            ConversationId = cmd.ConversationId,
            SenderId = cmd.SenderId,
            Content = cmd.Content,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Messages.Add(message);

        conversation.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return message.Id;
    }
}