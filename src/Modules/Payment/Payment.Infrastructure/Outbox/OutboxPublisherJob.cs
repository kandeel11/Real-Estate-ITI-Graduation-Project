using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Payment.Infrastructure.Persistence;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;

namespace Payment.Infrastructure.Outbox;

/// <summary>
/// Signal-driven outbox publisher.
///
/// Wakes up immediately when OutboxWriter.AddAsync() stages a new message
/// (via OutboxProcessingSignal). Falls back to a sweep every 2 hours as a
/// safety net in case a signal was missed.
///
/// This replaces the previous 10-second polling loop, which prevented Azure SQL
/// Free Tier from ever reaching its 1-hour auto-pause inactivity window.
/// </summary>
public sealed class OutboxPublisherJob : BackgroundService
{
    // Safety-net fallback: if no Stripe webhooks fire, sweep every 2 hours.
    // This allows Azure SQL to auto-pause between sweeps.
    private static readonly TimeSpan FallbackInterval = TimeSpan.FromHours(2);

    // Grace period after a signal: SaveChanges may not have committed yet when
    // OutboxWriter fires the notification. 500 ms covers all realistic cases.
    private static readonly TimeSpan CommitGrace = TimeSpan.FromMilliseconds(500);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboxProcessingSignal _signal;
    private readonly ILogger<OutboxPublisherJob> _logger;

    public OutboxPublisherJob(
        IServiceScopeFactory scopeFactory,
        OutboxProcessingSignal signal,
        ILogger<OutboxPublisherJob> logger)
    {
        _scopeFactory = scopeFactory;
        _signal = signal;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxPublisherJob started (signal-driven, {Fallback}h fallback).",
            FallbackInterval.TotalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            // Wait for a signal from OutboxWriter or the safety-net timeout.
            await _signal.WaitAsync(FallbackInterval, stoppingToken);

            if (stoppingToken.IsCancellationRequested)
                break;

            // Brief grace period so SaveChanges can commit before we query.
            await Task.Delay(CommitGrace, stoppingToken);

            try
            {
                await ProcessOutboxAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxPublisherJob failed during execution.");
            }
        }
    }

    private async Task ProcessOutboxAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var messages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedOn == null)
            .OrderBy(m => m.OccurredOn)
            .Take(20)
            .ToListAsync(ct);

        if (messages.Count == 0)
            return;

        _logger.LogInformation("OutboxPublisherJob: Found {Count} unprocessed messages.", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                if (message.Type.Contains(nameof(SubscriptionPaymentSucceededEvent)))
                {
                    var @event = JsonSerializer.Deserialize<SubscriptionPaymentSucceededEvent>(
                        message.Content, JsonOptions);
                    if (@event is not null)
                        await publisher.Publish(@event, ct);
                }
                else if (message.Type.Contains(nameof(RentPaymentSucceededEvent)))
                {
                    var @event = JsonSerializer.Deserialize<RentPaymentSucceededEvent>(
                        message.Content, JsonOptions);
                    if (@event is not null)
                        await publisher.Publish(@event, ct);
                }
                else
                {
                    _logger.LogWarning("OutboxPublisherJob: Unknown message type '{Type}'. Skipping.", message.Type);
                }

                message.MarkProcessed();
                _logger.LogInformation("OutboxPublisherJob: Published message {Id} ({Type})", message.Id, message.Type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "OutboxPublisherJob: Failed to publish message {Id} ({Type}). Will retry on next wake.",
                    message.Id, message.Type);
                message.MarkFailed(ex.Message);
            }
        }

        await dbContext.SaveChangesAsync(ct);
    }
}
