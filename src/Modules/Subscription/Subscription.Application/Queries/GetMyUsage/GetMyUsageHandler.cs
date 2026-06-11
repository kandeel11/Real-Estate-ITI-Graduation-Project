using MediatR;
using Subscription.Application.DTOs;
using Subscription.Application.Interfaces;

namespace Subscription.Application.Queries.GetMyUsage;

public class GetMyUsageHandler : IRequestHandler<GetMyUsageQuery, SubscriptionUsageDto?>
{
    private readonly ISubscriptionRepository _subscriptions;
    private readonly ISubscriptionUsageRepository _usage;

    public GetMyUsageHandler(
        ISubscriptionRepository subscriptions,
        ISubscriptionUsageRepository usage)
    {
        _subscriptions = subscriptions;
        _usage = usage;
    }

    public async Task<SubscriptionUsageDto?> Handle(GetMyUsageQuery request, CancellationToken cancellationToken)
    {
        var subscription = request.CompanyId is not null
            ? await _subscriptions.GetActiveByCompanyAsync(request.CompanyId.Value, cancellationToken)
            : await _subscriptions.GetActiveByUserAsync(request.UserId, cancellationToken);

        if (subscription is null) return null;

        var usage = await _usage.GetCurrentPeriodAsync(subscription.Id, cancellationToken);

        // No usage row yet (nothing consumed this period) — report zeros rather than 404
        if (usage is null)
        {
            return new SubscriptionUsageDto
            {
                Id = Guid.Empty,
                SubscriptionId = subscription.Id,
                PeriodStart = subscription.StartDate,
                PeriodEnd = subscription.EndDate,
                PropertiesUsed = 0,
                BrokersUsed = 0,
                AiRequestsUsed = 0,
                StorageUsedGb = 0,
            };
        }

        return new SubscriptionUsageDto
        {
            Id = usage.Id,
            SubscriptionId = usage.SubscriptionId,
            PeriodStart = usage.PeriodStart,
            PeriodEnd = usage.PeriodEnd,
            PropertiesUsed = usage.PropertiesUsed,
            BrokersUsed = usage.BrokersUsed,
            AiRequestsUsed = usage.AiRequestsUsed,
            StorageUsedGb = usage.StorageUsedGb,
        };
    }
}
