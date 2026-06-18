using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Payment.Application.Features.Commands.GenerateMonthlyPayments;

namespace Payment.Infrastructure.BackgroundJobs;

/// <summary>
/// Background Job بيشتغل أول يوم في كل شهر الساعة 6 الصبح
/// بيولد دفعات الإيجار الشهرية لكل الـ Active Leases أوتوماتيك
///
/// بيستخدم نفس الـ GenerateMonthlyPaymentsCommand اللي عملناه في الـ Application layer
/// </summary>
public sealed class MonthlyPaymentGenerationJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MonthlyPaymentGenerationJob> _logger;

    public MonthlyPaymentGenerationJob(
        IServiceScopeFactory scopeFactory,
        ILogger<MonthlyPaymentGenerationJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MonthlyPaymentGenerationJob started.");

        await WaitUntilFirstOfMonthAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await GeneratePaymentsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MonthlyPaymentGenerationJob failed.");
            }

            // انتظر حتى أول الشهر الجاي
            await WaitUntilFirstOfMonthAsync(stoppingToken);
        }
    }

    private async Task GeneratePaymentsAsync(CancellationToken ct)
    {
        var targetMonth = DateOnly.FromDateTime(DateTime.UtcNow);

        _logger.LogInformation(
            "MonthlyPaymentGenerationJob: Generating payments for {Month}/{Year}",
            targetMonth.Month, targetMonth.Year);

        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var result = await mediator.Send(
            new GenerateMonthlyPaymentsCommand(targetMonth), ct);

        _logger.LogInformation(
            "MonthlyPaymentGenerationJob: Done. Generated={Generated}, Skipped={Skipped}, Errors={Errors}",
            result.GeneratedCount, result.SkippedCount, result.Errors.Count);

        if (result.Errors.Any())
        {
            foreach (var error in result.Errors)
                _logger.LogError("MonthlyPaymentGenerationJob Error: {Error}", error);
        }
    }

    /// <summary>
    /// ينتظر حتى أول الشهر الجاي الساعة 6 AM UTC
    /// </summary>
    private static async Task WaitUntilFirstOfMonthAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var nextRun = new DateTime(now.Year, now.Month, 1, 6, 0, 0, DateTimeKind.Utc)
            .AddMonths(1);

        var delay = nextRun - now;

        if (delay < TimeSpan.Zero)
            delay = TimeSpan.Zero;

        await Task.Delay(delay, ct);
    }
}
