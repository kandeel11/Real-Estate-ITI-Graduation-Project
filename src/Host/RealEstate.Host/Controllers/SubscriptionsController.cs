using Identity.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Subscription.Application.Commands.CancelMySubscription;
using Subscription.Application.Commands.UpgradeMySubscription;
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

    /// <summary>
    /// Switches the caller's active subscription to another plan (catalog at
    /// /api/subscription-plans). Charging is Phase 2 — this moves the plan and
    /// raises SubscriptionUpgradedEvent for the Payment module.
    /// </summary>
    [HttpPost("mine/upgrade")]
    public async Task<IActionResult> Upgrade([FromBody] UpgradeSubscriptionRequest req, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId is null) return Unauthorized(new { error = "User not authenticated." });

        try
        {
            await mediator.Send(new UpgradeMySubscriptionCommand(userId.Value, currentUser.CompanyId, req.PlanId), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    /// <summary>
    /// Cancels a premium subscription — meaning the caller drops back to the
    /// FREE plan of the same audience (the subscription is never deleted).
    /// </summary>
    [HttpPost("mine/cancel")]
    public async Task<IActionResult> Cancel([FromBody] CancelSubscriptionRequest req, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId is null) return Unauthorized(new { error = "User not authenticated." });

        try
        {
            await mediator.Send(new CancelMySubscriptionCommand(userId.Value, currentUser.CompanyId, req.Reason ?? string.Empty), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }
}

public record UpgradeSubscriptionRequest(Guid PlanId);
public record CancelSubscriptionRequest(string? Reason);
