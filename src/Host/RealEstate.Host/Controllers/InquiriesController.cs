using Document.Application.Commands.Inquiry;
using Document.Application.DTOs;
using Document.Application.Queries.Inquiry;
using Document.Application.Queries.Lead.GetLeadByInquiry;
using Document.Application.Queries.Message.GetMessagesByConversation;
using Document.Application.Queries.Message.GetMessagesByInquiry;
using Identity.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Property.Application.Features.Queries.GetPropertyById;

namespace RealEstate.Host.Controllers;

[ApiController]
[Route("api/inquiries")]
[Authorize]
public class InquiriesController(
    IMediator mediator,
    ICurrentUserService currentUser,
    IUserRepository userRepo) : ControllerBase
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
        catch (InvalidOperationException ex)
        {
            // Idempotent: an already-accepted inquiry already has a lead — return it instead of 400.
            var existingLeadId = await mediator.Send(new GetLeadByInquiryQuery(id), ct);
            if (existingLeadId is not null)
                return Ok(new { leadId = existingLeadId });
            return BadRequest(new { error = ex.Message });
        }
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

    // ── Chat thread (inquiry-scoped messages) ────────────────────────────────────

    /// <summary>Returns the conversation thread for an inquiry, enriched for the chat UI.</summary>
    [HttpGet("{id:guid}/messages")]
    [Authorize(Roles = "tenant,owner,broker,company_manager")]
    public async Task<IActionResult> GetMessages(Guid id, CancellationToken ct)
    {
        var inquiry = await mediator.Send(new GetInquiryByIdQuery(id), ct);
        if (inquiry is null) return NotFound(new { error = $"Inquiry {id} not found." });

        var messages = await mediator.Send(new GetMessagesByInquiryQuery(id), ct);
        return Ok(await ToChatMessages(messages, inquiry, ct));
    }

    /// <summary>Post a chat message on an inquiry thread (either tenant or broker/owner).</summary>
    [HttpPost("{id:guid}/messages")]
    [Authorize(Roles = "tenant,owner,broker,company_manager")]
    public async Task<IActionResult> PostMessage(Guid id, [FromBody] ReplyInquiryBodyRequest req, CancellationToken ct)
    {
        try
        {
            var saved = await mediator.Send(new PostInquiryMessageCommand(id, UserId, req.Text), ct);
            var inquiry = await mediator.Send(new GetInquiryByIdQuery(id), ct);
            var mapped = (await ToChatMessages([saved], inquiry!, ct)).Single();
            return Ok(mapped);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    /// <summary>Maps raw message DTOs to the frontend ChatMessage shape with sender names/roles.</summary>
    private async Task<List<object>> ToChatMessages(
        IReadOnlyCollection<MessageDto> messages, InquiryResponse inquiry, CancellationToken ct)
    {
        if (messages.Count == 0) return new List<object>();

        var names = await userRepo.GetFullNamesByIdsAsync(
            messages.Select(m => m.SenderId).Distinct(), ct);

        return messages.Select(m => (object)new
        {
            id = m.Id,
            senderId = m.SenderId,
            senderName = names.GetValueOrDefault(m.SenderId, string.Empty),
            senderRole = m.SenderId == inquiry.TenantId ? "tenant"
                       : m.SenderId == inquiry.OwnerId ? "owner"
                       : "broker",
            text = m.Content,
            sentAt = m.CreatedAt
        }).ToList();
    }
}

public record CreateInquiryBodyRequest(Guid PropertyId, Guid OwnerId, Guid? CompanyId, string Message);
public record ReplyInquiryBodyRequest(string Text);