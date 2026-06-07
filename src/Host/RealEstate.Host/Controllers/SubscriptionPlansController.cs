using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Subscription.Application.Commands.CreatePlan;
using Subscription.Application.Commands.DeactivatePlan;
using Subscription.Application.Commands.UpdatePlanFeatures;
using Subscription.Application.Queries.GetActivePlans;
using Subscription.Application.Queries.GetAllPlans;
using Subscription.Application.Queries.GetPlanById;

namespace RealEstate.Host.Controllers;

[ApiController]
[Route("api/subscription-plans")]
public class SubscriptionPlansController : ControllerBase
{
    private readonly IMediator _mediator;

    public SubscriptionPlansController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetActive([FromQuery] string? audience, CancellationToken ct)
        => Ok(await _mediator.Send(new GetActivePlansQuery(audience), ct));

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var plan = await _mediator.Send(new GetPlanByIdQuery(id), ct);
        return plan is null ? NotFound() : Ok(plan);
    }

    [HttpGet("admin/all")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await _mediator.Send(new GetAllPlansQuery(), ct));

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreatePlanCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id:guid}/features")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateFeatures(Guid id, [FromBody] UpdatePlanFeaturesCommand command, CancellationToken ct)
    {
        var withId = command with { PlanId = id };
        await _mediator.Send(withId, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeactivatePlanCommand(id), ct);
        return NoContent();
    }
}
