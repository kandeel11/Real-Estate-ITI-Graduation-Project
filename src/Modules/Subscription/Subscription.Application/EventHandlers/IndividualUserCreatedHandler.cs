using MediatR;
using Microsoft.Extensions.Logging;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;
using Subscription.Application.Interfaces;
using Subscription.Domain.Entities;
using Subscription.Domain.Enums;

namespace Subscription.Application.EventHandlers;

public class IndividualUserCreatedHandler : INotificationHandler<IndividualUserCreatedEvent>
{
    private readonly ISubscriptionPlanRepository _planRepo;
    private readonly ISubscriptionRepository _subscriptionRepo;
    private readonly ISubscriptionUsageRepository _usageRepo;
    private readonly ILogger<IndividualUserCreatedHandler> _logger;

    public IndividualUserCreatedHandler(
        ISubscriptionPlanRepository planRepo,
        ISubscriptionRepository subscriptionRepo,
        ISubscriptionUsageRepository usageRepo,
        ILogger<IndividualUserCreatedHandler> logger)
    {
        _planRepo = planRepo;
        _subscriptionRepo = subscriptionRepo;
        _usageRepo = usageRepo;
        _logger = logger;
    }

    public async Task Handle(IndividualUserCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var tier = notification.AccountType switch
            {
                "Tenant" => SubscriptionTier.FreeTenant,
                "Owner"  => SubscriptionTier.FreeIndividual,
                "Broker" => SubscriptionTier.FreeIndividual,
                _ => throw new InvalidOperationException(
                    $"Unknown account type '{notification.AccountType}' for user {notification.UserId}")
            };

            var plan = await _planRepo.GetByTierAsync(tier, cancellationToken);
            if (plan is null)
            {
                _logger.LogError(
                    "Cannot assign subscription: plan with tier {Tier} not found in catalog", tier);
                return;
            }

            var now = DateTime.UtcNow;
            var subscription = Domain.Entities.Subscription.Create(
                planId: plan.Id,
                userId: notification.UserId,
                companyId: null,
                billingCycle: BillingCycle.Monthly,
                startDate: now,
                endDate: new DateTime(2099, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                trialEndDate: null);

            await _subscriptionRepo.CreateAsync(subscription, cancellationToken);

            var periodStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var periodEnd = periodStart.AddMonths(1).AddTicks(-1);
            var usage = SubscriptionUsage.Create(subscription.Id, periodStart, periodEnd);
            await _usageRepo.CreateAsync(usage, cancellationToken);

            _logger.LogInformation(
                "Subscription {SubId} ({Tier}) auto-assigned to user {UserId} ({AccountType})",
                subscription.Id, tier, notification.UserId, notification.AccountType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to assign subscription for {AccountType} user {UserId}",
                notification.AccountType, notification.UserId);
            // Don't rethrow — event handler failures must not break registration
        }
    }
}
