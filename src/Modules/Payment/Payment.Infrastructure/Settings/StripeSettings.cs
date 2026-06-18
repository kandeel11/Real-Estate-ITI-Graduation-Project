namespace Payment.Infrastructure.Settings;

/// <summary>
/// الـ Settings بتاعة Stripe — بتتقرأ من appsettings.json
/// 
/// في appsettings.json أضف:
/// "Stripe": {
///   "SecretKey": "sk_live_xxxx",         ← الـ key الحقيقي
///   "PublishableKey": "pk_live_xxxx",    ← بيتبعت للـ frontend
///   "WebhookSecret": "whsec_xxxx"        ← من Stripe Dashboard → Webhooks
/// }
/// 
/// في appsettings.Development.json أضف:
/// "Stripe": {
///   "SecretKey": "sk_test_xxxx",
///   "PublishableKey": "pk_test_xxxx",
///   "WebhookSecret": "whsec_xxxx"
/// }
/// </summary>
public sealed class StripeSettings
{
    public const string SectionName = "Stripe";

    /// <summary>
    /// Secret Key — لا تشاركه مع أي حد، بس على الـ backend
    /// </summary>
    public string SecretKey { get; init; } = string.Empty;

    /// <summary>
    /// Publishable Key — ممكن تبعته للـ frontend
    /// </summary>
    public string PublishableKey { get; init; } = string.Empty;

    /// <summary>
    /// Webhook Signing Secret — بتجيبه من Stripe Dashboard
    /// بيُستخدم للتحقق إن الـ webhook جاي من Stripe فعلاً
    /// </summary>
    public string WebhookSecret { get; init; } = string.Empty;

    /// <summary>
    /// العملة الافتراضية — الإمارات = aed
    /// </summary>
    public string DefaultCurrency { get; init; } = "aed";
}
