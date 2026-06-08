using Identity.Application.Interfaces;
using MediatR;
using Maintenance.Application.Features.Commands.AcceptAssignment;
using Maintenance.Application.Features.Commands.AddComment;
using Maintenance.Application.Features.Commands.AssignVendor;
using Maintenance.Application.Features.Commands.CancelTicket;
using Maintenance.Application.Features.Commands.CompleteWork;
using Maintenance.Application.Features.Commands.ConfirmPayment;
using Maintenance.Application.Features.Commands.CreatePayment;
using Maintenance.Application.Features.Commands.CreateTicket;
using Maintenance.Application.Features.Commands.DeleteTicket;
using Maintenance.Application.Features.Commands.MakeDecision;
using Maintenance.Application.Features.Commands.RejectAssignment;
using Maintenance.Application.Features.Commands.SubmitTicket;
using Maintenance.Application.Features.Commands.UpdateTicket;
using Maintenance.Application.Features.Commands.UploadAttachment;
using Maintenance.Application.Features.Queries.GetAiEvaluation;
using Maintenance.Application.Features.Queries.GetPayment;
using Maintenance.Application.Features.Queries.GetTicketById;
using Maintenance.Application.Features.Queries.GetTickets;
using Maintenance.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RealEstate.Host.Controllers;

[ApiController]
[Route("api/maintenance/tickets")]
[Authorize]
public class TicketsController(ISender sender, ICurrentUserService currentUser) : ControllerBase
{
    // ── helpers ────────────────────────────────────────────────────
    private Guid CurrentUserId => currentUser.UserId
        ?? throw new UnauthorizedAccessException("User ID not found in token.");

    // ── Ticket CRUD ────────────────────────────────────────────────

    /// <summary>Only tenants can open maintenance tickets.</summary>
    [HttpPost]
    [Authorize(Roles = "tenant")]
    public async Task<IActionResult> Create([FromBody] CreateTicketRequest req)
    {
        var id = await sender.Send(new CreateTicketCommand(
            TenantId: CurrentUserId,  
            PropertyId: req.PropertyId,
            OwnerId: req.OwnerId,
            LeaseId: req.LeaseId,
            Title: req.Title,
            Description: req.Description,
            Priority: req.Priority));

        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await sender.Send(new GetTicketByIdQuery(id));
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? propertyId,
        [FromQuery] TicketStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        // Automatically scope the list to the caller's role
        Guid? tenantId = currentUser.IsInRole("tenant") ? CurrentUserId : null;
        Guid? ownerId = currentUser.IsInRole("owner") ? CurrentUserId : null;

        var result = await sender.Send(
            new GetTicketsQuery(tenantId, ownerId, propertyId, status, page, pageSize));
        return Ok(result);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "tenant")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTicketCommand cmd)
    {
        await sender.Send(cmd with { TicketId = id });
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "tenant")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await sender.Send(new DeleteTicketCommand(id));
        return NoContent();
    }

    // ── Submit + AI ────────────────────────────────────────────────

    [HttpPost("{id:guid}/submit")]
    [Authorize(Roles = "tenant")]
    public async Task<IActionResult> Submit(Guid id, [FromBody] SubmitTicketRequest req)
    {
        await sender.Send(new SubmitTicketCommand(
            TicketId: id,
            TenantEmail: currentUser.Email ?? req.TenantEmail,  // prefer JWT email
            TenantPhone: req.TenantPhone,
            PropertyAddress: req.PropertyAddress));

        return Ok(new { message = "Ticket submitted. AI evaluation triggered automatically." });
    }

    [HttpGet("{id:guid}/ai-evaluation")]
    public async Task<IActionResult> GetAiEvaluation(Guid id)
    {
        var result = await sender.Send(new GetAiEvaluationQuery(id));
        return result is null ? NotFound("AI evaluation not yet available.") : Ok(result);
    }

    // ── Decision ───────────────────────────────────────────────────

    [HttpPost("{id:guid}/decision")]
    [Authorize(Roles = "owner,tenant")]
    public async Task<IActionResult> MakeDecision(Guid id, [FromBody] MakeDecisionRequest req)
    {
        await sender.Send(new MakeDecisionCommand(id, req.ResolutionMethod));
        return Ok(new { message = $"Decision recorded: {req.ResolutionMethod}" });
    }

    // ── Vendor Assignment ──────────────────────────────────────────

    [HttpPost("{id:guid}/assign-vendor")]
    [Authorize(Roles = "owner,tenant")]
    public async Task<IActionResult> AssignVendor(Guid id, [FromBody] AssignVendorRequest req)
    {
        var assignmentId = await sender.Send(new AssignVendorCommand(
            TicketId: id,
            VendorId: req.VendorId,
            AssignedById: CurrentUserId));  

        return Ok(new { assignmentId });
    }

    [HttpPost("assignments/{assignmentId:guid}/accept")]
    [Authorize(Roles = "maintenance_company")]
    public async Task<IActionResult> AcceptAssignment(Guid assignmentId)
    {
        await sender.Send(new AcceptAssignmentCommand(assignmentId));
        return Ok(new { message = "Assignment accepted." });
    }

    [HttpPost("assignments/{assignmentId:guid}/reject")]
    [Authorize(Roles = "maintenance_company")]
    public async Task<IActionResult> RejectAssignment(
        Guid assignmentId, [FromBody] RejectAssignmentRequest req)
    {
        await sender.Send(new RejectAssignmentCommand(assignmentId, req.Reason));
        return Ok(new { message = "Assignment rejected." });
    }

    // ── Work progress ──────────────────────────────────────────────

    [HttpPost("{id:guid}/complete-work")]
    [Authorize(Roles = "maintenance_company")]
    public async Task<IActionResult> CompleteWork(Guid id)
    {
        await sender.Send(new CompleteWorkCommand(id));
        return Ok(new { message = "Work marked as complete." });
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "tenant,owner")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        await sender.Send(new CancelTicketCommand(id, CurrentUserId));
        return Ok(new { message = "Ticket cancelled." });
    }

    // ── Attachments ────────────────────────────────────────────────

    [HttpPost("{id:guid}/attachments")]
    [Authorize(Roles = "tenant")]
    public async Task<IActionResult> UploadAttachment(Guid id, IFormFile file)
    {
        var attachmentId = await sender.Send(
            new UploadAttachmentCommand(id, file, CurrentUserId));  

        return Ok(new { attachmentId });
    }

    // ── Comments ───────────────────────────────────────────────────

    [HttpPost("{id:guid}/comments")]
    [Authorize(Roles = "tenant,owner,maintenance_company")]
    public async Task<IActionResult> AddComment(Guid id, [FromBody] AddCommentRequest req)
    {
        var role = currentUser.Role switch
        {
            "tenant" => Maintenance.Domain.Enums.CommentAuthorRole.Tenant,
            "owner" => Maintenance.Domain.Enums.CommentAuthorRole.Owner,
            "maintenance_company" => Maintenance.Domain.Enums.CommentAuthorRole.Vendor,
            _ => Maintenance.Domain.Enums.CommentAuthorRole.Tenant
        };

        var commentId = await sender.Send(new AddCommentCommand(
            TicketId: id,
            UserId: CurrentUserId, 
            UserRole: role,            
            Content: req.Content,
            IsInternal: req.IsInternal));

        return Ok(new { commentId });
    }

    // ── Payments ───────────────────────────────────────────────────

    [HttpPost("{id:guid}/payments")]
    [Authorize(Roles = "tenant,owner")]
    public async Task<IActionResult> CreatePayment(Guid id, [FromBody] CreatePaymentRequest req)
    {
        var paymentId = await sender.Send(new CreatePaymentCommand(
            TicketId: id,
            PayerId: CurrentUserId,   // ← from JWT, not body
            VendorId: req.VendorId,
            Amount: req.Amount,
            Currency: req.Currency,
            PaymentMethod: req.PaymentMethod));

        return Ok(new { paymentId });
    }

    [HttpPost("payments/{paymentId:guid}/confirm")]
    [Authorize(Roles = "tenant,owner")]
    public async Task<IActionResult> ConfirmPayment(
        Guid paymentId, [FromBody] ConfirmPaymentRequest req)
    {
        await sender.Send(new ConfirmPaymentCommand(paymentId, req.TransactionId));
        return Ok(new { message = "Payment confirmed. Ticket marked as Completed." });
    }

    [HttpGet("{id:guid}/payments")]
    public async Task<IActionResult> GetPayment(Guid id)
    {
        var result = await sender.Send(new GetPaymentQuery(id));
        return result is null ? NotFound() : Ok(result);
    }
}

public record CreateTicketRequest(
    Guid PropertyId, Guid OwnerId, Guid? LeaseId,
    string Title, string Description, string Priority);

public record SubmitTicketRequest(
    string PropertyAddress, string TenantPhone,
    string TenantEmail = ""); 

public record MakeDecisionRequest(ResolutionMethod ResolutionMethod);
public record AssignVendorRequest(Guid VendorId);
public record RejectAssignmentRequest(string Reason);
public record AddCommentRequest(string Content, bool IsInternal = false);
public record CreatePaymentRequest(Guid VendorId, decimal Amount, string Currency = "SAR", string? PaymentMethod = null);
public record ConfirmPaymentRequest(string TransactionId);