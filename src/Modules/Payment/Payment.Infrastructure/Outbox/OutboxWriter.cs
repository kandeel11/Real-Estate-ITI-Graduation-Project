using System.Text.Json;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure.Outbox;

/// <summary>
/// Adds an outbox message to the current DbContext unit-of-work (no SaveChanges —
/// that is the caller's responsibility). Notifies OutboxProcessingSignal after
/// staging the message so the publisher can wake up as soon as SaveChanges commits.
/// </summary>
public sealed class OutboxWriter : IOutboxWriter
{
    private readonly PaymentDbContext _context;
    private readonly OutboxProcessingSignal _signal;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public OutboxWriter(PaymentDbContext context, OutboxProcessingSignal signal)
    {
        _context = context;
        _signal = signal;
    }

    public async Task AddAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        var message = OutboxMessage.Create(
            type: typeof(TEvent).FullName ?? typeof(TEvent).Name,
            content: JsonSerializer.Serialize(integrationEvent, JsonOptions));

        await _context.OutboxMessages.AddAsync(message, cancellationToken);

        // Signal the publisher. The message isn't committed yet (SaveChanges is the
        // caller's job), so the publisher adds a short grace delay before querying.
        _signal.Notify();
    }
}
