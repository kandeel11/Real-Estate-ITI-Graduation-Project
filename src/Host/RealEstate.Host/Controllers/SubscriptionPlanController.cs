using MediatR;
using Microsoft.AspNetCore.Mvc;
using Subscription.Application.Commands.CreatePlan;
using Subscription.Application.Commands.DeactivatePlan;
using Subscription.Application.Commands.UpdatePlanFeatures;
using Subscription.Application.Queries.GetActivePlans;
using Subscription.Application.Queries.GetAllPlans;
using Subscription.Application.Queries.GetPlanById;

namespace RealEstate.Host.Controllers;

/// <summary>
/// إدارة خطط الاشتراك (SubscriptionPlan + PlanFeature)
/// الموديول ده ما يعرفش أي شيء عن الدفع — راجع PaymentController في Payment module
/// لعمليات الـ Checkout/Refund/إلخ.
/// </summary>
[ApiController]
[Route("api/subscription-plans")]
public sealed class SubscriptionPlanController : ControllerBase
{
    private readonly IMediator _mediator;

    public SubscriptionPlanController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAllPlansQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("active")]
    public async Task<ActionResult> GetActive(
        [FromQuery] string? audience,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetActivePlansQuery(audience), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{planId:guid}")]
    public async Task<ActionResult> GetById(Guid planId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPlanByIdQuery(planId), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(
        [FromBody] CreatePlanCommand command,
        CancellationToken cancellationToken)
    {
        var planId = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { planId }, planId);
    }

    [HttpPut("{planId:guid}/features")]
    public async Task<ActionResult> UpdateFeatures(
        Guid planId,
        [FromBody] UpdatePlanFeaturesRequestBody body,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new UpdatePlanFeaturesCommand(
                planId,
                body.HasAdvancedAnalytics,
                body.HasFeaturedListings,
                body.HasAiPriceEstimation,
                body.HasAiChatbot,
                body.HasAiLeadPrioritization,
                body.HasAiDescriptionGenerator),
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{planId:guid}/deactivate")]
    public async Task<ActionResult> Deactivate(Guid planId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeactivatePlanCommand(planId), cancellationToken);
        return NoContent();
    }
}

public sealed record UpdatePlanFeaturesRequestBody(
    bool HasAdvancedAnalytics,
    bool HasFeaturedListings,
    bool HasAiPriceEstimation,
    bool HasAiChatbot,
    bool HasAiLeadPrioritization,
    bool HasAiDescriptionGenerator);
