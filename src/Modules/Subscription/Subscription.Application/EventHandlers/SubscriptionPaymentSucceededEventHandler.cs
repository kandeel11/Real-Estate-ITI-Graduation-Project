using MediatR;
using Microsoft.Extensions.Logging;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;
using Subscription.Application.Interfaces;

namespace Subscription.Application.EventHandlers;

/// <summary>
/// بيستقبل SubscriptionPaymentSucceededEvent اللي بينشره Payment module
/// (عبر Outbox → MediatR Publish) بعد تأكيد نجاح الدفع من Stripe webhook
///
/// الخطوات:
/// 1. يجيب الـ Subscription بـ SubscriptionId
/// 2. يتأكد إنه في حالة PendingPayment (تجنب التفعيل المزدوج لو الـ event اتكرر)
/// 3. subscription.Activate(startDate, endDate)
/// 4. يحفظ التغيير
///
/// ده اللي يقفل الحلقة في Subscription Payment Flow:
/// User → Subscription (PendingPayment) → Payment → Stripe → Webhook → Event → Subscription (Active)
/// </summary>
public sealed class SubscriptionPaymentSucceededEventHandler
    : INotificationHandler<SubscriptionPaymentSucceededEvent>
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ILogger<SubscriptionPaymentSucceededEventHandler> _logger;

    public SubscriptionPaymentSucceededEventHandler(
        ISubscriptionRepository subscriptionRepository,
        ILogger<SubscriptionPaymentSucceededEventHandler> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _logger = logger;
    }

    public async Task Handle(SubscriptionPaymentSucceededEvent notification, CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(notification.SubscriptionId, cancellationToken);

        if (subscription is null)
        {
            _logger.LogWarning(
                "SubscriptionPaymentSucceededEvent: Subscription {SubscriptionId} not found. PaymentId={PaymentId}",
                notification.SubscriptionId, notification.PaymentId);
            return;
        }

        if (subscription.Status != Domain.Enums.SubscriptionStatus.PendingPayment)
        {
            _logger.LogInformation(
                "SubscriptionPaymentSucceededEvent: Subscription {SubscriptionId} is already '{Status}'. Skipping activation (idempotency).",
                notification.SubscriptionId, subscription.Status);
            return;
        }

        // Supersede the caller's previous active subscription (the one being
        // replaced by this paid upgrade). Without this there would be two
        // Active rows for the same user/company and "get active" would return
        // an arbitrary one — leaving the plan looking unchanged after payment.
        var previous = subscription.CompanyId is not null
            ? await _subscriptionRepository.GetActiveByCompanyAsync(subscription.CompanyId.Value, cancellationToken)
            : await _subscriptionRepository.GetActiveByUserAsync(subscription.UserId, cancellationToken);

        if (previous is not null && previous.Id != subscription.Id)
        {
            previous.Expire();
            await _subscriptionRepository.UpdateAsync(previous, cancellationToken);
            _logger.LogInformation(
                "Superseded previous subscription {PreviousId} (plan {PreviousPlanId}) on upgrade to {SubscriptionId}.",
                previous.Id, previous.PlanId, subscription.Id);
        }

        var startDate = DateTime.UtcNow;
        var endDate = CalculateExpiryDate(startDate, subscription.BillingCycle);

        subscription.Activate(startDate, endDate);

        await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);

        _logger.LogInformation(
            "Subscription {SubscriptionId} activated after successful payment {PaymentId}. Plan={PlanId}, EndDate={EndDate}",
            notification.SubscriptionId, notification.PaymentId, notification.PlanId, endDate);
    }

    /// <summary>
    /// يحسب تاريخ انتهاء الاشتراك بناءً على Billing Cycle
    /// </summary>
    private static DateTime CalculateExpiryDate(DateTime startDate, Domain.Enums.BillingCycle billingCycle) =>
        billingCycle switch
        {
            Domain.Enums.BillingCycle.Monthly => startDate.AddMonths(1),
            Domain.Enums.BillingCycle.Quarterly => startDate.AddMonths(3),
            Domain.Enums.BillingCycle.Annual => startDate.AddYears(1),
            _ => startDate.AddMonths(1)
        };
}
