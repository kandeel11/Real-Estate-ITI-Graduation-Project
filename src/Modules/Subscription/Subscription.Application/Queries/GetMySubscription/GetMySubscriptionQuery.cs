using MediatR;
using Subscription.Application.DTOs;

namespace Subscription.Application.Queries.GetMySubscription;

/// <summary>
/// Resolves the caller's active subscription: company subscription when the
/// caller belongs to a company (manager/broker), otherwise their personal one.
/// </summary>
public record GetMySubscriptionQuery(Guid UserId, Guid? CompanyId) : IRequest<MySubscriptionDto?>;
