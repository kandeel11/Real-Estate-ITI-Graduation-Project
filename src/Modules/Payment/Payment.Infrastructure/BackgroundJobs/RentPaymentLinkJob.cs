using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payment.Application.Interfaces;

namespace Payment.Infrastructure.BackgroundJobs;

/// <summary>
/// بيشتغل كل يوم الساعة 8 الصبح (UTC) — ساعة قبل OverduePaymentsJob.
///
/// بيدور على كل دفعات الإيجار القادمة خلال نافذة {DaysBeforeDue} أيام
/// اللي لسه معملتلهاش Stripe Checkout Link وبيولدلهم واحد أوتوماتيك.
///
/// ── السيناريو ──────────────────────────────────────────────────────────────
/// تاريخ بداية الإيجار: 4/6 (يونيو)
/// التردد: Bimonthly (كل شهرين)
/// يوم الدفع: 10
/// →  الفترة 1: 4/6 → 3/8  — دفعة أول مرة نقدًا (Cash)
/// →  الفترة 2: 4/8 → 3/10 — أول دفعة أونلاين، تاريخ استحقاق 10/8
///    يوم 8/8:  RentPaymentLinkJob يولد Stripe Checkout Link لـ 10/8
///    يوم 10/8: المستأجر يدفع أونلاين
///    يوم 11/8: لو ما دفعش → OverduePaymentsJob يطبق غرامة حسب إعدادات Admin
/// ─────────────────────────────────────────────────────────────────────────
/// </summary>
public sealed class RentPaymentLinkJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RentPaymentLinkJob> _logger;
    private readonly RentLinkJobSettings _settings;

    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    public RentPaymentLinkJob(
        IServiceScopeFactory scopeFactory,
        ILogger<RentPaymentLinkJob> logger,
        IOptions<RentLinkJobSettings> settings)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
        _settings     = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RentPaymentLinkJob started.");

        // انتظر حتى الـ 8 AM UTC
        await WaitUntilNextRunAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await GenerateLinksAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RentPaymentLinkJob failed during execution.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    private async Task GenerateLinksAsync(CancellationToken ct)
    {
        var today     = DateOnly.FromDateTime(DateTime.UtcNow);
        var windowEnd = today.AddDays(_settings.DaysBeforeDue);

        _logger.LogInformation(
            "RentPaymentLinkJob: Looking for rent payments due {From}–{To} without a Stripe link…",
            today, windowEnd);

        using var scope       = _scopeFactory.CreateScope();
        var repository        = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
        var stripeCheckout    = scope.ServiceProvider.GetRequiredService<IStripeCheckoutService>();
        var leaseService      = scope.ServiceProvider.GetRequiredService<ILeaseService>();

        var upcoming = await repository.GetUpcomingRentPaymentsWithoutLinkAsync(today, windowEnd, ct);

        if (upcoming.Count == 0)
        {
            _logger.LogInformation("RentPaymentLinkJob: No upcoming payments need a Stripe link today.");
            return;
        }

        _logger.LogInformation(
            "RentPaymentLinkJob: {Count} payment(s) need Stripe Checkout links.", upcoming.Count);

        int generated = 0;

        foreach (var payment in upcoming)
        {
            try
            {
                // اجلب بيانات الـ lease عشان نعرف اسم المستأجر والـ context
                var lease = await leaseService.GetLeaseByIdAsync(payment.LeaseId, ct);

                var result = await stripeCheckout.CreateCheckoutSessionAsync(new(
                    PaymentId:   payment.Id,
                    Amount:      payment.Amount + payment.LateFee,
                    ProductName: $"Rent Payment — {payment.DueDate:MMMM yyyy}",
                    SuccessUrl:  _settings.SuccessUrl,
                    CancelUrl:   _settings.CancelUrl,
                    Metadata: new Dictionary<string, string>
                    {
                        ["payment_id"]  = payment.Id.ToString(),
                        ["lease_id"]    = payment.LeaseId.ToString(),
                        ["due_date"]    = payment.DueDate.ToString("yyyy-MM-dd"),
                        ["type"]        = "rent",
                        ["tenant_id"]   = lease?.TenantId.ToString() ?? string.Empty,
                        ["property_id"] = lease?.PropertyId.ToString() ?? string.Empty
                    }
                ), ct);

                payment.SetCheckoutSession(result.SessionId, result.Url);
                generated++;

                _logger.LogInformation(
                    "RentPaymentLinkJob: Stripe link generated for Payment {Id} " +
                    "(DueDate={DueDate}, Amount={Amount}, LeaseId={LeaseId})",
                    payment.Id, payment.DueDate, payment.Amount, payment.LeaseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "RentPaymentLinkJob: Failed to generate Stripe link for Payment {Id}", payment.Id);
            }
        }

        await repository.SaveChangesAsync(ct);

        _logger.LogInformation(
            "RentPaymentLinkJob: Done — {Generated}/{Total} link(s) generated.",
            generated, upcoming.Count);
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>ينتظر حتى الـ 8 AM UTC في اليوم القادم (ساعة قبل OverduePaymentsJob)</summary>
    private static async Task WaitUntilNextRunAsync(CancellationToken ct)
    {
        var now     = DateTime.UtcNow;
        var nextRun = now.Date.AddHours(8); // 8 AM UTC

        if (now >= nextRun)
            nextRun = nextRun.AddDays(1);

        var delay = nextRun - now;
        await Task.Delay(delay, ct);
    }
}

/// <summary>
/// إعدادات الـ RentPaymentLinkJob من appsettings.json (section: "Payment:RentLinkJob")
///
/// مثال appsettings:
/// "Payment": {
///   "RentLinkJob": {
///     "DaysBeforeDue": 2,
///     "SuccessUrl": "https://app.domain.com/tenant/payments/success",
///     "CancelUrl":  "https://app.domain.com/tenant/payments"
///   }
/// }
/// </summary>
public sealed class RentLinkJobSettings
{
    public const string SectionName = "Payment:RentLinkJob";

    /// <summary>
    /// كام يوم قبل تاريخ الاستحقاق يتولد الـ Stripe link؟ (افتراضي: 2 أيام)
    /// مثال: DueDate=10/8 → link يتولد في 8/8
    /// </summary>
    public int DaysBeforeDue { get; init; } = 2;

    /// <summary>صفحة النجاح في الـ Angular app بعد الدفع</summary>
    public string SuccessUrl { get; init; } = "http://localhost:4200/tenant/payments/success";

    /// <summary>صفحة الإلغاء في الـ Angular app</summary>
    public string CancelUrl { get; init; } = "http://localhost:4200/tenant/payments";
}
