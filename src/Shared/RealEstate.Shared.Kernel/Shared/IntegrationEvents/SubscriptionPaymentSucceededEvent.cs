using MediatR;

namespace RealEstate.Shared.Kernel.Shared.IntegrationEvents;

/// <summary>
/// Integration Event يتم نشره من Payment module بعد نجاح دفع الاشتراك
/// (Stripe webhook → checkout.session.completed → payment.MarkPaid())
///
/// Subscription module بيستقبل هذا الـ event عبر:
///   INotificationHandler&lt;SubscriptionPaymentSucceededEvent&gt;
/// وبيعمل subscription.Activate() + يحدد StartDate/EndDate.
///
/// ملحوظة: هذا الملف ينتمي لمشروع RealEstate.Shared.Kernel
/// (المشترك بين كل الموديولات) ولم يكن موجوداً ضمن الزيب المرفوع — أضفناه هنا
/// كجزء من التسليم، ولازم يُنسخ لمكانه الحقيقي:
/// Shared/RealEstate.Shared.Kernel/Shared/IntegrationEvents/SubscriptionPaymentSucceededEvent.cs
/// </summary>
public record SubscriptionPaymentSucceededEvent(
    Guid SubscriptionId,
    Guid PaymentId,
    Guid UserId,
    Guid PlanId,
    decimal Amount
) : INotification;
