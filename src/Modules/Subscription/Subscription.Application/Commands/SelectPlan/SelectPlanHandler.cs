using MediatR;
using Subscription.Application.Interfaces;
using Subscription.Domain.Entities;

namespace Subscription.Application.Commands.SelectPlan;

/// <summary>
/// Handler بتاع SelectPlanCommand
/// بينشئ Subscription بحالة PendingPayment — مش Active
/// (هي دي النقطة المهمة في الـ refactor: الاشتراك ما يتفعّلش غير
/// بعد ما Payment module يأكد نجاح الدفع عبر SubscriptionPaymentSucceededEvent)
/// </summary>
public sealed class SelectPlanHandler : IRequestHandler<SelectPlanCommand, SelectPlanResult>
{
    private readonly ISubscriptionPlanRepository _planRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;

    public SelectPlanHandler(
        ISubscriptionPlanRepository planRepository,
        ISubscriptionRepository subscriptionRepository)
    {
        _planRepository = planRepository;
        _subscriptionRepository = subscriptionRepository;
    }

    public async Task<SelectPlanResult> Handle(SelectPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await _planRepository.GetByIdAsync(request.PlanId, cancellationToken)
            ?? throw new InvalidOperationException($"Plan {request.PlanId} not found.");

        if (!plan.IsActive)
            throw new InvalidOperationException($"Plan '{plan.Name}' is not currently active.");

        var subscription = Domain.Entities.Subscription.CreatePendingPayment(
            planId: request.PlanId,
            userId: request.UserId,
            companyId: request.CompanyId,
            billingCycle: request.BillingCycle);

        await _subscriptionRepository.CreateAsync(subscription, cancellationToken);

        return new SelectPlanResult(
            SubscriptionId: subscription.Id,
            PlanId: subscription.PlanId,
            BillingCycle: subscription.BillingCycle,
            Status: subscription.Status.ToString());
    }
}
