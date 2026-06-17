using MediatR;
using Microsoft.AspNetCore.Mvc;
using Subscription.Application.Commands.SelectPlan;
using Subscription.Domain.Enums;

namespace RealEstate.Host.Controllers;
/// <summary>
/// أول خطوة في Subscription Payment Flow:
/// المستخدم يختار خطة → بينشئ Subscription بحالة PendingPayment
/// بعد كده الـ Frontend بيستدعي:
///   POST /api/payments/subscriptions/checkout
/// (في Payment module) باستخدام SubscriptionId اللي رجع من هنا
/// عشان يحصل على Stripe Checkout URL.
/// </summary>
[ApiController]
[Route("api/subscriptions")]
public sealed class SubscriptionController : ControllerBase
{
    private readonly IMediator _mediator;

    public SubscriptionController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("select-plan")]
    public async Task<ActionResult<SelectPlanResult>> SelectPlan(
        [FromBody] SelectPlanRequestBody body,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(
                new SelectPlanCommand(body.UserId, body.CompanyId, body.PlanId, body.BillingCycle),
                cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public sealed record SelectPlanRequestBody(
    Guid UserId,
    Guid? CompanyId,
    Guid PlanId,
    BillingCycle BillingCycle);
