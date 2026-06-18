using Document.Application.Commands.Lease.ExpireDueLeases;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace RealEstate.Host.Jobs;

/// <summary>
/// Replaces the Hangfire RecurringJob for lease expiry.
///
/// Sleeps until the next UTC midnight, runs ExpireDueLeasesCommand, then sleeps
/// until the following midnight. No SQL polling between runs — the DB can auto-pause
/// for the entire 24-hour window between sweeps.
///
/// The command is idempotent, so a missed midnight (app down at 00:00) is safe:
/// leases will be expired on the next sweep with no duplicates.
/// </summary>
public sealed class LeaseExpiryBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LeaseExpiryBackgroundService> _logger;

    public LeaseExpiryBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<LeaseExpiryBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = TimeUntilNextMidnightUtc();
            _logger.LogInformation("LeaseExpiryBackgroundService: next sweep in {Hours:F1}h (at UTC midnight).",
                delay.TotalHours);

            await Task.Delay(delay, stoppingToken);

            if (stoppingToken.IsCancellationRequested)
                break;

            await RunSweepAsync(stoppingToken);
        }
    }

    private async Task RunSweepAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var expired = await mediator.Send(new ExpireDueLeasesCommand(), ct);
            if (expired > 0)
                _logger.LogInformation("Lease expiry sweep ended {Count} due lease(s).", expired);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LeaseExpiryBackgroundService sweep failed.");
        }
    }

    private static TimeSpan TimeUntilNextMidnightUtc()
    {
        var now = DateTime.UtcNow;
        var nextMidnight = now.Date.AddDays(1);
        return nextMidnight - now;
    }
}
