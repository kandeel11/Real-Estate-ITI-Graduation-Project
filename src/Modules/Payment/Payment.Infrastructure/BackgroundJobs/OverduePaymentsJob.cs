using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payment.Application.Interfaces;
using Payment.Infrastructure.Settings;

namespace Payment.Infrastructure.BackgroundJobs;

/// <summary>
/// Background Job بيشتغل كل يوم الساعة 9 الصبح
/// بيجيب الدفعات المتأخرة ويطبق عليها غرامة تأخير أوتوماتيك
/// وبيبعت SMS للمستأجرين كتذكير
///
/// بيتسجل كـ Hosted Service في PaymentModuleExtensions
/// </summary>
public sealed class OverduePaymentsJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OverduePaymentsJob> _logger;
    private readonly OverdueJobSettings _settings;

    // بيشتغل مرة كل 24 ساعة
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    public OverduePaymentsJob(
        IServiceScopeFactory scopeFactory,
        ILogger<OverduePaymentsJob> logger,
        IOptions<OverdueJobSettings> settings)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OverduePaymentsJob started.");

        // انتظر حتى الـ start time المحدد (9 AM بتوقيت UTC)
        await WaitUntilNextRunAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOverduePaymentsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OverduePaymentsJob failed during execution.");
            }

            // انتظر 24 ساعة للـ run الجاي
            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task ProcessOverduePaymentsAsync(CancellationToken ct)
    {
        _logger.LogInformation("OverduePaymentsJob: Starting overdue check at {Time}", DateTime.UtcNow);

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var leaseService = scope.ServiceProvider.GetRequiredService<ILeaseService>();

        var overduePayments = await repository.GetOverdueAsync(ct);

        if (!overduePayments.Any())
        {
            _logger.LogInformation("OverduePaymentsJob: No overdue payments found.");
            return;
        }

        _logger.LogInformation(
            "OverduePaymentsJob: Found {Count} overdue payments. Processing...",
            overduePayments.Count);

        int processed = 0;
        int notified = 0;

        foreach (var payment in overduePayments)
        {
            try
            {
                // تطبيق غرامة التأخير لو مش متطبقة
                if (payment.LateFee == 0)
                {
                    var daysOverdue = (DateOnly.FromDateTime(DateTime.UtcNow).DayNumber - payment.DueDate.DayNumber);
                    var lateFee = CalculateLateFee(payment.Amount, daysOverdue);

                    payment.ApplyLateFee(lateFee);
                    processed++;

                    _logger.LogDebug(
                        "Applied late fee {Fee} AED to Payment {Id} ({Days} days overdue)",
                        lateFee, payment.Id, daysOverdue);
                }

                // إرسال SMS تذكير للمستأجر
                if (_settings.SendSmsReminders)
                {
                    var lease = await leaseService.GetLeaseByIdAsync(payment.LeaseId, ct);
                    if (lease?.TenantPhone is not null)
                    {
                        await notificationService.SendSmsAsync(new(
                            PhoneNumber: lease.TenantPhone,
                            Message: $"تذكير: دفعة الإيجار بتاريخ {payment.DueDate:dd/MM/yyyy} " +
                                     $"متأخرة. المبلغ: {payment.Amount + payment.LateFee:N2} AED. " +
                                     "يرجى السداد لتجنب غرامات إضافية."
                        ), ct);
                        notified++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OverduePaymentsJob: Error processing Payment {Id}", payment.Id);
            }
        }

        await repository.SaveChangesAsync(ct);

        _logger.LogInformation(
            "OverduePaymentsJob: Completed. LateFees applied={Processed}, SMS sent={Notified}",
            processed, notified);
    }

    /// <summary>
    /// حساب غرامة التأخير:
    /// - أول 7 أيام: 2% من المبلغ
    /// - بعد 7 أيام: 5% من المبلغ
    /// - الحد الأقصى: 15% من المبلغ
    /// </summary>
    private static decimal CalculateLateFee(decimal amount, int daysOverdue)
    {
        var percentage = daysOverdue <= 7 ? 0.02m : 0.05m;
        var fee = amount * percentage;
        var maxFee = amount * 0.15m; // 15% حد أقصى
        return Math.Min(fee, maxFee);
    }

    /// <summary>
    /// ينتظر حتى الـ 9 AM UTC في اليوم الجاي
    /// </summary>
    private static async Task WaitUntilNextRunAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var nextRun = now.Date.AddHours(9); // 9 AM UTC

        if (now > nextRun)
            nextRun = nextRun.AddDays(1);

        var delay = nextRun - now;
        await Task.Delay(delay, ct);
    }
}

/// <summary>
/// إعدادات الـ OverduePaymentsJob من appsettings.json
/// </summary>
public sealed class OverdueJobSettings
{
    public const string SectionName = "Payment:OverdueJob";

    public bool Enabled { get; init; } = true;
    public bool SendSmsReminders { get; init; } = true;
}
