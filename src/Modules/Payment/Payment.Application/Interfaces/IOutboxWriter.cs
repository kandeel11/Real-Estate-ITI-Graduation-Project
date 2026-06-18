namespace Payment.Application.Interfaces;

/// <summary>
/// يكتب Integration Events في outbox table — هتُنشر بعد كده عبر Background Job
///
/// ليه Outbox Pattern؟
/// عشان نضمن إن نشر الـ event يحصل بشكل موثوق (at-least-once)، حتى لو
/// السيرفر اتقفل فجأة بعد ما الـ Payment اتحدث في الداتابيز.
/// الكتابة في الـ Outbox بتحصل في نفس الـ DB transaction بتاعة الـ payment update.
/// </summary>
public interface IOutboxWriter
{
    /// <summary>
    /// يضيف رسالة جديدة للـ outbox — لازم SaveChangesAsync بعدها
    /// من نفس الـ Unit of Work عشان تُحفظ في نفس المعاملة
    /// </summary>
    Task AddAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : class;
}
