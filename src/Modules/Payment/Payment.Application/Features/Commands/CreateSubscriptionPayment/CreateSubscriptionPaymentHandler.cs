using MediatR;
using Microsoft.Extensions.Logging;
using Payment.Application.Exceptions;
using Payment.Application.Interfaces;
using Payment.Domain.Enums;

namespace Payment.Application.Features.Commands.CreateSubscriptionPayment;

/// <summary>
/// Handler بتاع CreateSubscriptionPaymentCommand
///
/// الخطوات:
/// 1. يجيب تفاصيل الخطة من Subscription module (عبر ISubscriptionPlanService)
/// 2. يحدد المبلغ بناءً على BillingCycle (Monthly/Quarterly/Annual)
/// 3. ينشئ Payment بـ PaymentType=Subscription, Status=Pending
/// 4. يربط SubscriptionPaymentDetails (SubscriptionId, PlanId, BillingCycle)
/// 5. ينشئ Stripe Checkout Session
/// 6. يحفظ CheckoutSessionId + CheckoutUrl على الـ Payment
/// 7. يحفظ كل ده في معاملة واحدة (Unit of Work)
///
/// ملاحظة: الـ Subscription بتكون أُنشئت مسبقاً بحالة PendingPayment
/// عبر Subscription module قبل ما يتم إرسال هذا الـ Command.
/// </summary>
public sealed class CreateSubscriptionPaymentHandler
    : IRequestHandler<CreateSubscriptionPaymentCommand, CreateSubscriptionPaymentResult>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ISubscriptionPlanService _subscriptionPlanService;
    private readonly IStripeCheckoutService _stripeCheckoutService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateSubscriptionPaymentHandler> _logger;

    public CreateSubscriptionPaymentHandler(
        IPaymentRepository paymentRepository,
        ISubscriptionPlanService subscriptionPlanService,
        IStripeCheckoutService stripeCheckoutService,
        IUnitOfWork unitOfWork,
        ILogger<CreateSubscriptionPaymentHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _subscriptionPlanService = subscriptionPlanService;
        _stripeCheckoutService = stripeCheckoutService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CreateSubscriptionPaymentResult> Handle(
        CreateSubscriptionPaymentCommand request,
        CancellationToken cancellationToken)
    {
        // 1. جيب تفاصيل الخطة من Subscription module (SubscriptionDbContext عبر الـ adapter)
        var plan = await _subscriptionPlanService.GetPlanByIdAsync(request.PlanId, cancellationToken)
            ?? throw new PlanNotFoundException(request.PlanId);

        if (!plan.IsActive)
            throw new InvalidOperationException($"Plan '{plan.Name}' is not currently active.");

        // 2. حدد المبلغ بناءً على Billing Cycle
        var amount = request.BillingCycle switch
        {
            BillingCycle.Monthly => plan.MonthlyPrice,
            BillingCycle.Quarterly => plan.QuarterlyPrice,
            BillingCycle.Annual => plan.AnnualPrice,
            _ => plan.MonthlyPrice
        };

        // 3 + 4. أنشئ سجل الدفع (Pending) مع تفاصيل الاشتراك المرتبطة
        var payment = Domain.Entities.Payment.CreateSubscription(
            subscriptionId: request.SubscriptionId,
            planId: request.PlanId,
            billingCycle: request.BillingCycle,
            amount: amount,
            notes: $"Subscription payment - Plan: {plan.Name} ({request.BillingCycle})");

        // 5. أنشئ Stripe Checkout Session
        var checkoutSession = await _stripeCheckoutService.CreateCheckoutSessionAsync(
            new CreateCheckoutSessionRequest(
                PaymentId: payment.Id,
                Amount: amount,
                ProductName: $"{plan.Name} Subscription ({request.BillingCycle})",
                SuccessUrl: request.SuccessRedirectUrl ?? "https://www.google.com",
                CancelUrl: request.CancelRedirectUrl ?? "https://www.google.com",
                Metadata: new Dictionary<string, string>
                {
                    ["payment_id"] = payment.Id.ToString(),
                    ["payment_type"] = nameof(PaymentType.Subscription),
                    ["user_id"] = request.UserId.ToString(),
                    ["subscription_id"] = request.SubscriptionId.ToString(),
                    ["plan_id"] = request.PlanId.ToString()
                }),
            cancellationToken);

        // 6. خزّن بيانات الـ Checkout على الـ Payment
        payment.SetCheckoutSession(checkoutSession.SessionId, checkoutSession.Url);

        // 7. حفظ كل ده في معاملة واحدة
        await _paymentRepository.AddAsync(payment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Subscription payment {PaymentId} created for User={UserId}, Subscription={SubscriptionId}, Plan={PlanId}. CheckoutSession={SessionId}",
            payment.Id, request.UserId, request.SubscriptionId, request.PlanId, checkoutSession.SessionId);

        return new CreateSubscriptionPaymentResult(
            PaymentId: payment.Id,
            CheckoutUrl: checkoutSession.Url,
            CheckoutSessionId: checkoutSession.SessionId);
    }
}
