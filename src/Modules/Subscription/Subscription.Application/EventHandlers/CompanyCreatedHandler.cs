using MediatR;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;
using Subscription.Application.Interfaces;
using Subscription.Domain.Entities;
using Subscription.Domain.Enums;

namespace Subscription.Application.EventHandlers;

public class CompanyCreatedHandler : INotificationHandler<CompanyCreatedEvent>
{
    private readonly ISubscriptionPlanRepository _planRepo;
    private readonly ISubscriptionRepository _subscriptionRepo;
    private readonly ISubscriptionUsageRepository _usageRepo;

    public CompanyCreatedHandler(
        ISubscriptionPlanRepository planRepo,
        ISubscriptionRepository subscriptionRepo,
        ISubscriptionUsageRepository usageRepo)
    {
        _planRepo = planRepo;
        _subscriptionRepo = subscriptionRepo;
        _usageRepo = usageRepo;
    }

    public async Task Handle(CompanyCreatedEvent notification, CancellationToken cancellationToken)
    {
        var freePlan = await _planRepo.GetByTierAsync(SubscriptionTier.FreeCompany, cancellationToken)
            ?? throw new InvalidOperationException("FreeCompany plan not found. Ensure seed data has run.");

        var now = DateTime.UtcNow;
        var subscription = Domain.Entities.Subscription.Create(
            planId: freePlan.Id,
            userId: notification.AdminUserId,
            companyId: notification.CompanyId,
            billingCycle: BillingCycle.Monthly,
            startDate: now,
            endDate: new DateTime(2099, 12, 31, 23, 59, 59, DateTimeKind.Utc),
            trialEndDate: null);

        await _subscriptionRepo.CreateAsync(subscription, cancellationToken);

        var periodStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = periodStart.AddMonths(1).AddTicks(-1);
        var usage = SubscriptionUsage.Create(subscription.Id, periodStart, periodEnd);
        await _usageRepo.CreateAsync(usage, cancellationToken);
    }
}
