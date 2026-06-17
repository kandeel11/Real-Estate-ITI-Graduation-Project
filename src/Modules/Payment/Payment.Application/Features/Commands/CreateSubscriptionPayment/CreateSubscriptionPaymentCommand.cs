using MediatR;
using Payment.Domain.Enums;

namespace Payment.Application.Features.Commands.CreateSubscriptionPayment;

/// <summary>
/// Command: يبدأ عملية دفع اشتراك (جديد / تجديد / ترقية)
/// النتيجة بترجع Stripe Checkout URL يتم تحويل المستخدم له لإكمال الدفع
/// </summary>
public sealed record CreateSubscriptionPaymentCommand(
    Guid UserId,
    Guid SubscriptionId,
    Guid PlanId,
    BillingCycle BillingCycle,

    string? SuccessRedirectUrl = null,
    string? CancelRedirectUrl = null
) : IRequest<CreateSubscriptionPaymentResult>;

public sealed record CreateSubscriptionPaymentResult(
    Guid PaymentId,
    string CheckoutUrl,
    string CheckoutSessionId
);
