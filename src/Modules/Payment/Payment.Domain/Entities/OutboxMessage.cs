namespace Payment.Domain.Entities;

/// <summary>
/// كيان رسائل الـ Outbox — بيخزن Integration Events قبل نشرها
/// عشان نضمن إن الحدث يتنشر حتى لو السيرفر اتقفل فجأة بعد الحفظ
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public DateTime OccurredOn { get; private set; }
    public DateTime? ProcessedOn { get; private set; }
    public string? Error { get; private set; }

    private OutboxMessage() { }

    public static OutboxMessage Create(string type, string content)
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = type,
            Content = content,
            OccurredOn = DateTime.UtcNow
        };
    }

    public void MarkProcessed() => ProcessedOn = DateTime.UtcNow;

    public void MarkFailed(string error) => Error = error;
}
