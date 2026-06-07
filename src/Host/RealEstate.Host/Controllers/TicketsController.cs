using MediatR;
using Microsoft.AspNetCore.Mvc;
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

namespace RealEstate.Host.Controllers;

[ApiController]
[Route("api/maintenance/tickets")]
public class TicketsController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTicketCommand cmd)
    {
        var id = await sender.Send(cmd);
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
        [FromQuery] Guid? tenantId, [FromQuery] Guid? ownerId,
        [FromQuery] Guid? propertyId, [FromQuery] TicketStatus? status,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await sender.Send(new GetTicketsQuery(tenantId, ownerId, propertyId, status, page, pageSize));
        return Ok(result);
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTicketCommand cmd)
    {
        await sender.Send(cmd with { TicketId = id });
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await sender.Send(new DeleteTicketCommand(id));
        return NoContent();
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, [FromBody] SubmitTicketCommand cmd)
    {
        await sender.Send(cmd with { TicketId = id });
        return Ok(new { message = "Ticket submitted. AI evaluation triggered automatically." });
    }

    [HttpGet("{id:guid}/ai-evaluation")]
    public async Task<IActionResult> GetAiEvaluation(Guid id)
    {
        var result = await sender.Send(new GetAiEvaluationQuery(id));
        return result is null ? NotFound("AI evaluation not yet available.") : Ok(result);
    }

    [HttpPost("{id:guid}/decision")]
    public async Task<IActionResult> MakeDecision(Guid id, [FromBody] MakeDecisionCommand cmd)
    {
        await sender.Send(cmd with { TicketId = id });
        return Ok(new { message = $"Decision recorded: {cmd.ResolutionMethod}" });
    }

    [HttpPost("{id:guid}/assign-vendor")]
    public async Task<IActionResult> AssignVendor(Guid id, [FromBody] AssignVendorCommand cmd)
    {
        var assignmentId = await sender.Send(cmd with { TicketId = id });
        return Ok(new { assignmentId });
    }

    [HttpPost("assignments/{assignmentId:guid}/accept")]
    public async Task<IActionResult> AcceptAssignment(Guid assignmentId)
    {
        await sender.Send(new AcceptAssignmentCommand(assignmentId));
        return Ok(new { message = "Assignment accepted." });
    }

    [HttpPost("assignments/{assignmentId:guid}/reject")]
    public async Task<IActionResult> RejectAssignment(Guid assignmentId, [FromBody] RejectAssignmentCommand cmd)
    {
        await sender.Send(cmd with { AssignmentId = assignmentId });
        return Ok(new { message = "Assignment rejected." });
    }

    [HttpPost("{id:guid}/complete-work")]
    public async Task<IActionResult> CompleteWork(Guid id)
    {
        await sender.Send(new CompleteWorkCommand(id));
        return Ok(new { message = "Work marked as complete." });
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelRequest req)
    {
        await sender.Send(new CancelTicketCommand(id, req.CancelledBy));
        return Ok(new { message = "Ticket cancelled." });
    }

    [HttpPost("{id:guid}/attachments")]
    public async Task<IActionResult> UploadAttachment(Guid id, IFormFile file, [FromQuery] Guid uploadedById)
    {
        var attachmentId = await sender.Send(new UploadAttachmentCommand(id, file, uploadedById));
        return Ok(new { attachmentId });
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<IActionResult> AddComment(Guid id, [FromBody] AddCommentCommand cmd)
    {
        var commentId = await sender.Send(cmd with { TicketId = id });
        return Ok(new { commentId });
    }

    [HttpPost("{id:guid}/payments")]
    public async Task<IActionResult> CreatePayment(Guid id, [FromBody] CreatePaymentCommand cmd)
    {
        var paymentId = await sender.Send(cmd with { TicketId = id });
        return Ok(new { paymentId });
    }

    [HttpPost("payments/{paymentId:guid}/confirm")]
    public async Task<IActionResult> ConfirmPayment(Guid paymentId, [FromBody] ConfirmPaymentCommand cmd)
    {
        await sender.Send(cmd with { PaymentId = paymentId });
        return Ok(new { message = "Payment confirmed. Ticket marked as Completed." });
    }

    [HttpGet("{id:guid}/payments")]
    public async Task<IActionResult> GetPayment(Guid id)
    {
        var result = await sender.Send(new GetPaymentQuery(id));
        return result is null ? NotFound() : Ok(result);
    }
}

public record CancelRequest(string CancelledBy);