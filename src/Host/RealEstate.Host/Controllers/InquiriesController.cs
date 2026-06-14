using Document.Application.Commands.Inquiry;
using Document.Application.DTOs;
using Document.Application.Queries.Inquiry;
using Identity.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Property.Application.Features.Queries.GetPropertyById;

namespace RealEstate.Host.Controllers;

[ApiController]
[Route("api/inquiries")]
[Authorize]
public class InquiriesController(IMediator mediator, ICurrentUserService currentUser) : ControllerBase
{
    private Guid UserId => currentUser.UserId ?? throw new UnauthorizedAccessException();

    [HttpPost]
    [Authorize(Roles = "tenant")]
    public async Task<IActionResult> Create([FromBody] CreateInquiryBodyRequest req, CancellationToken ct)
    {
        try
        {
            // Route the inquiry to the property's listing broker (if any) so it
            // surfaces in their dashboard and they get notified.
            var property = await mediator.Send(new GetPropertyByIdQuery(req.PropertyId), ct);

            // FIX: TenantId from JWT, not body
            var result = await mediator.Send(new CreateInquiryCommand(
                req.PropertyId, UserId, req.OwnerId, req.CompanyId, req.Message,
                property?.ListingBrokerId), ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            // Plan inquiry limit reached
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetInquiryByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    // FIX: was GET /tenant/{tenantId} — now /mine scoped from JWT
    [HttpGet("mine")]
    [Authorize(Roles = "tenant")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
        => Ok(await mediator.Send(new GetMyInquiriesQuery(UserId), ct));

    [HttpGet("property/{propertyId:guid}")]
    public async Task<IActionResult> GetByProperty(Guid propertyId, CancellationToken ct)
        => Ok(await mediator.Send(new GetInquiriesByPropertyQuery(propertyId), ct));

    [HttpPut("{id:guid}/accept")]
    [Authorize(Roles = "owner,broker,company_manager")]
    public async Task<IActionResult> Accept(Guid id, CancellationToken ct)
    {
        try { return Ok(await mediator.Send(new AcceptInquiryCommand(id, UserId), ct)); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    /// <summary>Convert an inquiry into a lead (accepts it, creating a lead + conversation).</summary>
    [HttpPost("{id:guid}/convert-to-lead")]
    [Authorize(Roles = "owner,broker,company_manager")]
    public async Task<IActionResult> ConvertToLead(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new AcceptInquiryCommand(id, UserId), ct);
            return Ok(new { leadId = result.LeadId });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    /// <summary>Broker/owner replies to an inquiry (creates a conversation message + notifies tenant).</summary>
    [HttpPost("{id:guid}/reply")]
    [Authorize(Roles = "owner,broker,company_manager")]
    public async Task<IActionResult> Reply(Guid id, [FromBody] ReplyInquiryBodyRequest req, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new ReplyToInquiryCommand(id, UserId, req.Text), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPut("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, CancellationToken ct)
    {
        try { await mediator.Send(new CloseInquiryCommand(id), ct); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
    }
}

public record CreateInquiryBodyRequest(Guid PropertyId, Guid OwnerId, Guid? CompanyId, string Message);
public record ReplyInquiryBodyRequest(string Text);