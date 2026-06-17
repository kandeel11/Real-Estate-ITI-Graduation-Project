using MediatR;
using Subscription.Application.DTOs;

namespace Subscription.Application.Queries.GetMyUsage;

/// <summary>
/// Current-period usage for the caller's active subscription (company-scoped
/// when the caller belongs to a company). Null when no active subscription.
/// </summary>
public record GetMyUsageQuery(Guid UserId, Guid? CompanyId) : IRequest<SubscriptionUsageDto?>;
