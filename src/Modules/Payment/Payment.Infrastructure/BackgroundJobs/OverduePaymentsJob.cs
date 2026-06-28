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
    private readonly OverdueJobSettings   _settings;
    private readonly RentLinkJobSettings  _linkSettings;

    // بيشتغل مرة كل 24 ساعة
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    public OverduePaymentsJob(
        IServiceScopeFactory scopeFactory,
        ILogger<OverduePaymentsJob> logger,
        IOptions<OverdueJobSettings> settings,
        IOptions<RentLinkJobSettings> linkSettings)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
        _settings     = settings.Value;
        _linkSettings = linkSettings.Value;
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
        var repository          = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var leaseService        = scope.ServiceProvider.GetRequiredService<ILeaseService>();
        var stripeCheckout      = scope.ServiceProvider.GetRequiredService<IStripeCheckoutService>();

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

                    // Regenerate checkout link with updated amount (base + late fee + 1% commission)
                    try
                    {
                        var lease = await leaseService.GetLeaseByIdAsync(payment.LeaseId, ct);
                        var totalWithCommission = Math.Round((payment.Amount + payment.LateFee) * 1.01m, 2);

                        var result = await stripeCheckout.CreateCheckoutSessionAsync(new(
                            PaymentId:   payment.Id,
                            Amount:      totalWithCommission,
                            ProductName: $"Rent Payment — {payment.DueDate:MMMM yyyy} (includes late fee)",
                            SuccessUrl:  _linkSettings.SuccessUrl,
                            CancelUrl:   _linkSettings.CancelUrl,
                            Metadata: new Dictionary<string, string>
                            {
                                ["payment_id"]  = payment.Id.ToString(),
                                ["lease_id"]    = payment.LeaseId.ToString(),
                                ["due_date"]    = payment.DueDate.ToString("yyyy-MM-dd"),
                                ["type"]        = "rent",
                                ["tenant_id"]   = lease?.TenantId.ToString() ?? string.Empty,
                                ["property_id"] = lease?.PropertyId.ToString() ?? string.Empty,
                                ["company_id"]  = lease?.CompanyId?.ToString() ?? string.Empty
                            },
                            IdempotencySuffix: $"_lf{daysOverdue}"
                        ), ct);

                        payment.SetCheckoutSession(result.SessionId, result.Url);

                        _logger.LogInformation(
                            "OverduePaymentsJob: Stripe link regenerated for Payment {Id} with late fee included (amount={Amount})",
                            payment.Id, totalWithCommission);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "OverduePaymentsJob: Failed to regenerate Stripe link for Payment {Id}. Late fee still applied.",
                            payment.Id);
                    }
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
    /// حساب غرامة التأخير بناءً على الإعدادات القابلة للتهيئة من Admin:
    /// - أول {Tier1Days} أيام: {Tier1Percent}% من المبلغ (افتراضي: 7 أيام × 2%)
    /// - بعد {Tier1Days} أيام: {Tier2Percent}% من المبلغ (افتراضي: 5%)
    /// - بعد فترة السماح {GracePeriodDays}: لا غرامة (افتراضي: 0 أيام)
    /// - الحد الأقصى: {MaxPercent}% من المبلغ (افتراضي: 15%)
    /// </summary>
    private decimal CalculateLateFee(decimal amount, int daysOverdue)
    {
        // فترة السماح — لا تطبق غرامة خلالها
        if (daysOverdue <= _settings.LateFeeGracePeriodDays)
            return 0m;

        var effectiveDays = daysOverdue - _settings.LateFeeGracePeriodDays;

        var pct = effectiveDays <= _settings.LateFeeTier1Days
            ? _settings.LateFeeTier1Percent / 100m
            : _settings.LateFeeTier2Percent / 100m;

        var fee    = amount * pct;
        var maxFee = amount * (_settings.LateFeeMaxPercent / 100m);
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
/// إعدادات الـ OverduePaymentsJob من appsettings.json (section: "Payment:OverdueJob")
/// قابلة للتعديل بواسطة Admin بدون إعادة نشر التطبيق.
///
/// مثال appsettings:
/// "Payment": {
///   "OverdueJob": {
///     "Enabled": true,
///     "SendSmsReminders": true,
///     "LateFeeGracePeriodDays": 0,
///     "LateFeeTier1Days": 7,
///     "LateFeeTier1Percent": 2.0,
///     "LateFeeTier2Percent": 5.0,
///     "LateFeeMaxPercent": 15.0
///   }
/// }
/// </summary>
public sealed class OverdueJobSettings
{
    public const string SectionName = "Payment:OverdueJob";

    public bool Enabled { get; init; } = true;
    public bool SendSmsReminders { get; init; } = true;

    // ── Late-fee configuration (Admin-tunable) ──────────────────────────────
    /// <summary>أيام السماح قبل تطبيق أي غرامة (0 = بدون سماح)</summary>
    public int LateFeeGracePeriodDays { get; init; } = 0;

    /// <summary>عدد أيام الشريحة الأولى من التأخير (default 7)</summary>
    public int LateFeeTier1Days { get; init; } = 7;

    /// <summary>نسبة الغرامة في الشريحة الأولى بالـ % (default 2%)</summary>
    public decimal LateFeeTier1Percent { get; init; } = 2.0m;

    /// <summary>نسبة الغرامة في الشريحة الثانية بالـ % (default 5%)</summary>
    public decimal LateFeeTier2Percent { get; init; } = 5.0m;

    /// <summary>الحد الأقصى للغرامة بالـ % من المبلغ الأصلي (default 15%)</summary>
    public decimal LateFeeMaxPercent { get; init; } = 15.0m;
}
