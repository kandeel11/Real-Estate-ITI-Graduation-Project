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
/// Background Job بيشتغل كل 10 ثواني
/// بيقرأ outbox messages اللي لسه مش processed
/// وبيـpublish الـ Integration Events للـ subscribers (مثل Subscription module)
///
/// لو الـ publish فشل، الرسالة تفضل unprocessed (ProcessedOn=null)
/// وهيتعاد المحاولة في الـ run الجاي — at-least-once delivery
/// </summary>
public sealed class OutboxPublisherJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxPublisherJob> _logger;

    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public OutboxPublisherJob(IServiceScopeFactory scopeFactory, ILogger<OutboxPublisherJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxPublisherJob started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxPublisherJob failed during execution.");
            }

            await Task.Delay(Interval, stoppingToken);
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

                _logger.LogInformation(
                    "OutboxPublisherJob: Published message {Id} ({Type})", message.Id, message.Type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "OutboxPublisherJob: Failed to publish message {Id} ({Type}). Will retry next run.",
                    message.Id, message.Type);

                message.MarkFailed(ex.Message);
            }
        }

        await dbContext.SaveChangesAsync(ct);
    }
}
