using System.Text.Json;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure.Outbox;

/// <summary>
/// Implementation بتاع IOutboxWriter
///
/// مهم: الدالة دي بس بتعمل AddAsync على الـ DbContext —
/// مش بتعمل SaveChanges. الـ SaveChanges بيحصل من الـ Caller
/// (عادةً UnitOfWork) عشان تُحفظ الرسالة في نفس الـ transaction
/// بتاعة تحديث الـ Payment.
/// </summary>
public sealed class OutboxWriter : IOutboxWriter
{
    private readonly PaymentDbContext _context;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public OutboxWriter(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        var message = OutboxMessage.Create(
            type: typeof(TEvent).FullName ?? typeof(TEvent).Name,
            content: JsonSerializer.Serialize(integrationEvent, JsonOptions));

        await _context.OutboxMessages.AddAsync(message, cancellationToken);
    }
}
