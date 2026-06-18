using MediatR;
using Subscription.Domain.Enums;

namespace Subscription.Application.Commands.SelectPlan;

/// <summary>
/// Command: المستخدم اختار خطة (جديدة أو ترقية) — أول خطوة في Subscription Payment Flow
///
/// النتيجة بترجع SubscriptionId بس — الـ Checkout URL بيتولد من خلال
/// استدعاء CreateSubscriptionPaymentCommand (في Payment module) بعد كده
/// من الـ Controller/BFF layer، باستخدام الـ SubscriptionId اللي رجع هنا.
///
/// السبب في الفصل ده: Subscription module ما ينفع يستدعي Payment module
/// مباشرة (modular boundary) — التنسيق بينهم بيتم على مستوى الـ API/Controller
/// أو عبر MediatR pipeline في الـ host project.
/// </summary>
public sealed record SelectPlanCommand(
    Guid UserId,
    Guid? CompanyId,
    Guid PlanId,
    BillingCycle BillingCycle
) : IRequest<SelectPlanResult>;

public sealed record SelectPlanResult(
    Guid SubscriptionId,
    Guid PlanId,
    BillingCycle BillingCycle,
    string Status
);
