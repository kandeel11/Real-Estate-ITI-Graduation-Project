using Identity.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Subscription.Application.Commands.CancelMySubscription;
using Subscription.Application.Commands.SelectPlan;
using Subscription.Application.Commands.UpgradeMySubscription;
using Subscription.Application.Queries.GetMySubscription;
using Subscription.Application.Queries.GetMyUsage;
using Subscription.Domain.Enums;

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

    /// <summary>
    /// Step 1 of the subscription payment flow: user selects a plan → creates a
    /// Subscription with status PendingPayment. Follow up with
    /// POST /api/payments/subscriptions/checkout using the returned SubscriptionId.
    /// </summary>
    [HttpPost("select-plan")]
    [AllowAnonymous]
    public async Task<ActionResult<SelectPlanResult>> SelectPlan(
        [FromBody] SelectPlanRequestBody body,
        CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(
                new SelectPlanCommand(body.UserId, body.CompanyId, body.PlanId, body.BillingCycle), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public record UpgradeSubscriptionRequest(Guid PlanId);
public record CancelSubscriptionRequest(string? Reason);
public sealed record SelectPlanRequestBody(Guid UserId, Guid? CompanyId, Guid PlanId, BillingCycle BillingCycle);
