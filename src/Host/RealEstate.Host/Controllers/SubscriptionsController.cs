using Identity.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Subscription.Application.Queries.GetMyPayments;
using Subscription.Application.Queries.GetMySubscription;
using Subscription.Application.Queries.GetMyUsage;

namespace RealEstate.Host.Controllers;

/// <summary>
/// The caller's own subscription. Company managers/brokers resolve to their
/// company's subscription (CompanyId from JWT); individuals to their personal one.
/// Plan catalog lives in SubscriptionPlansController (/api/subscription-plans).
/// </summary>
[ApiController]
[Route("api/subscriptions")]
[Authorize]
public class SubscriptionsController(IMediator mediator, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>
    /// Returns the caller's active subscription with plan limits and features.
    /// </summary>
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId is null) return Unauthorized(new { error = "User not authenticated." });

        var result = await mediator.Send(
            new GetMySubscriptionQuery(userId.Value, currentUser.CompanyId), ct);

        return result is null
            ? NotFound(new { error = "No active subscription found." })
            : Ok(result);
    }

    /// <summary>
    /// Returns current-period usage counters for the caller's active subscription.
    /// </summary>
    [HttpGet("mine/usage")]
    public async Task<IActionResult> GetMyUsage(CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId is null) return Unauthorized(new { error = "User not authenticated." });

        var result = await mediator.Send(
            new GetMyUsageQuery(userId.Value, currentUser.CompanyId), ct);

        return result is null
            ? NotFound(new { error = "No active subscription found." })
            : Ok(result);
    }

    /// <summary>
    /// Returns payment history (invoices) for the caller's active subscription.
    /// Empty list = subscribed but nothing charged yet (e.g. free plans).
    /// </summary>
    [HttpGet("mine/payments")]
    public async Task<IActionResult> GetMyPayments(CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId is null) return Unauthorized(new { error = "User not authenticated." });

        var result = await mediator.Send(
            new GetMyPaymentsQuery(userId.Value, currentUser.CompanyId), ct);

        return result is null
            ? NotFound(new { error = "No active subscription found." })
            : Ok(result);
    }
}
