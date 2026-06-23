using Document.Application.Commands.Inquiry;
using Document.Application.DTOs;
using Document.Application.Queries.Inquiry;
using Document.Application.Queries.Lead.GetLeadByInquiry;
using Document.Application.Queries.Message.GetInquiryLastMessages;
using Document.Application.Queries.Message.GetMessagesByConversation;
using Document.Application.Queries.Message.GetMessagesByInquiry;
using Document.Infrastructure.Hubs;
using Identity.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Property.Application.Features.Queries.GetPropertyById;

namespace RealEstate.Host.Controllers;

[ApiController]
[Route("api/inquiries")]
[Authorize]
public class InquiriesController(
    IMediator mediator,
    ICurrentUserService currentUser,
    IUserRepository userRepo,
    IHubContext<DocumentChatHub> chatHub) : ControllerBase
{
    private Guid UserId => currentUser.UserId ?? throw new UnauthorizedAccessException();

    /// <summary>Returns a 403 result if the caller hasn't verified their email, else null.</summary>
    private IActionResult? RequireVerified() =>
        currentUser.IsEmailVerified
            ? null
            : StatusCode(StatusCodes.Status403Forbidden,
                new { error = "Please verify your email to manage inquiries." });

    [HttpPost]
    [Authorize(Roles = "tenant")]
    public async Task<IActionResult> Create([FromBody] CreateInquiryBodyRequest req, CancellationToken ct)
    {
        if (!currentUser.IsEmailVerified)
            return StatusCode(StatusCodes.Status403Forbidden,
                new { error = "Please verify your email to send inquiries." });

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
            // Blocked by the one-engagement-at-a-time gate or the plan inquiry limit.
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>Tells the tenant whether they may send a new inquiry right now, and if not, why.</summary>
    [HttpGet("can-create")]
    [Authorize(Roles = "tenant")]
    public async Task<IActionResult> CanCreate(CancellationToken ct)
    {
        var gate = await mediator.Send(new GetTenantInquiryGateQuery(UserId), ct);
        return Ok(new { allowed = gate.Allowed, reason = gate.Message, reasonCode = gate.ReasonCode });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetInquiryByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    // FIX: was GET /tenant/{tenantId} — now /mine scoped from JWT.
    // Enriched for the chat list: property title, last message + time, newest-first.
    [HttpGet("mine")]
    [Authorize(Roles = "tenant")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var inquiries = await mediator.Send(new GetMyInquiriesQuery(UserId), ct);
        if (inquiries.Count == 0) return Ok(Array.Empty<object>());

        var inquiryIds = inquiries.Select(i => i.Id).ToList();
        var lastMessages = await mediator.Send(new GetInquiryLastMessagesQuery(inquiryIds), ct);
        // "Answered" = a broker/owner has replied, whether via the inquiry tab or the chat.
        var answered = await mediator.Send(new GetAnsweredInquiriesQuery(inquiryIds), ct);

        var titles = new Dictionary<Guid, string>();
        foreach (var pid in inquiries.Select(i => i.PropertyId).Distinct())
        {
            var prop = await mediator.Send(new GetPropertyByIdQuery(pid), ct);
            titles[pid] = prop?.Title ?? string.Empty;
        }

        var result = inquiries
            .OrderByDescending(i => lastMessages.GetValueOrDefault(i.Id)?.At ?? i.CreatedAt)
            .Select(i =>
            {
                var last = lastMessages.GetValueOrDefault(i.Id);
                var status = i.Status == "Accepted" ? "converted"
                           : answered.Contains(i.Id) ? "replied"
                           : "new";
                return (object)new
                {
                    id = i.Id,
                    propertyId = i.PropertyId,
                    propertyTitle = titles.GetValueOrDefault(i.PropertyId, string.Empty),
                    message = i.Message,
                    status,
                    createdAt = i.CreatedAt,
                    lastMessage = last?.Text ?? i.Message,
                    lastMessageAt = last?.At ?? i.CreatedAt
                };
            })
            .ToList();

        return Ok(result);
    }

    [HttpGet("property/{propertyId:guid}")]
    public async Task<IActionResult> GetByProperty(Guid propertyId, CancellationToken ct)
        => Ok(await mediator.Send(new GetInquiriesByPropertyQuery(propertyId), ct));

    [HttpPut("{id:guid}/accept")]
    [Authorize(Roles = "owner,broker,company_manager")]
    public async Task<IActionResult> Accept(Guid id, CancellationToken ct)
    {
        if (RequireVerified() is { } forbid) return forbid;
        try { return Ok(await mediator.Send(new AcceptInquiryCommand(id, UserId), ct)); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    /// <summary>Convert an inquiry into a lead (accepts it, creating a lead + conversation).</summary>
    [HttpPost("{id:guid}/convert-to-lead")]
    [Authorize(Roles = "owner,broker,company_manager")]
    public async Task<IActionResult> ConvertToLead(Guid id, CancellationToken ct)
    {
        if (RequireVerified() is { } forbid) return forbid;
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
        if (RequireVerified() is { } forbid) return forbid;
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
        var chat = await ToChatMessages(messages, inquiry, ct);

        // The tenant's original inquiry is the first message of every thread.
        var tenantNames = await userRepo.GetFullNamesByIdsAsync(new[] { inquiry.TenantId }, ct);
        chat.Insert(0, new
        {
            id = inquiry.Id,
            senderId = inquiry.TenantId,
            senderName = tenantNames.GetValueOrDefault(inquiry.TenantId, string.Empty),
            senderRole = "tenant",
            text = inquiry.Message,
            sentAt = DateTime.SpecifyKind(inquiry.CreatedAt, DateTimeKind.Utc)
        });

        return Ok(chat);
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

            // Push the new message live to everyone viewing this inquiry thread.
            // The sender also receives this echo and dedupes it by message id.
            await chatHub.Clients
                .Group(DocumentChatHub.InquiryGroup(id))
                .SendAsync("ReceiveMessage", new { inquiryId = id, message = mapped }, ct);

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
            sentAt = DateTime.SpecifyKind(m.CreatedAt, DateTimeKind.Utc)
        }).ToList();
    }
}

public record CreateInquiryBodyRequest(Guid PropertyId, Guid OwnerId, Guid? CompanyId, string Message);
public record ReplyInquiryBodyRequest(string Text);