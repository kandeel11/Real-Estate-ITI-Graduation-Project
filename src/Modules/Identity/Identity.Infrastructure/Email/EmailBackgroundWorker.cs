using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Email;

public sealed class EmailBackgroundWorker : BackgroundService
{
    private static readonly TimeSpan[] RetryDelays =
        [TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60)];

    private const int MaxAttempts = 3;

    private readonly ChannelReader<EmailJobWorkItem> _reader;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailBackgroundWorker> _logger;

    public EmailBackgroundWorker(
        Channel<EmailJobWorkItem> channel,
        IServiceScopeFactory scopeFactory,
        ILogger<EmailBackgroundWorker> logger)
    {
        _reader = channel.Reader;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var item in _reader.ReadAllAsync(stoppingToken))
        {
            await ProcessWithRetryAsync(item, stoppingToken);
        }
    }

    private async Task ProcessWithRetryAsync(EmailJobWorkItem item, CancellationToken ct)
    {
        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                await DispatchAsync(item, ct);
                return;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex) when (attempt < MaxAttempts)
            {
                var delay = RetryDelays[attempt - 1];
                _logger.LogWarning(ex,
                    "Email job {Type} for user {UserId} failed on attempt {Attempt}/{Max}. Retrying in {Delay}s.",
                    item.Type, item.UserId, attempt, MaxAttempts, delay.TotalSeconds);
                await Task.Delay(delay, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Email job {Type} for user {UserId} failed permanently after {Max} attempts.",
                    item.Type, item.UserId, MaxAttempts);
            }
        }
    }

    private async Task DispatchAsync(EmailJobWorkItem item, CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var jobs = scope.ServiceProvider.GetRequiredService<IEmailJobs>();

        switch (item.Type)
        {
            case EmailJobType.Otp:
                await jobs.SendOtpAsync(item.UserId, item.OtpCode!);
                break;
            case EmailJobType.VerificationLink:
                await jobs.SendVerificationLinkAsync(item.UserId);
                break;
            case EmailJobType.Welcome:
                await jobs.SendWelcomeAsync(item.UserId);
                break;
            case EmailJobType.PasswordReset:
                await jobs.SendPasswordResetAsync(item.UserId, item.ResetToken!);
                break;
            default:
                _logger.LogWarning("Unknown email job type {Type} for user {UserId} — skipped.", item.Type, item.UserId);
                break;
        }
    }
}
