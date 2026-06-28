using Identity.Application.Interfaces;
using MaintenanceCompanies.Application.Interfaces;
using Maintenance.Application.Features.Commands.AcceptAssignment;
using Maintenance.Application.Features.Commands.AddComment;
using Maintenance.Application.Features.Commands.AssignVendor;
using Maintenance.Application.Features.Commands.CancelTicket;
using Maintenance.Application.Features.Commands.CompleteWork;
using Maintenance.Application.Features.Commands.MarkCompleted;
using Maintenance.Application.Features.Commands.ConfirmPayment;
using Maintenance.Application.Features.Commands.CreateMaintenanceCheckout;
using Maintenance.Application.Features.Commands.CreatePayment;
using Maintenance.Application.Features.Commands.UserPay;
using Maintenance.Application.Features.Queries.GetCompanyRoutedTickets;
using Maintenance.Application.Features.Commands.CreateTicket;
using Maintenance.Application.Features.Commands.DeleteTicket;
using Maintenance.Application.Features.Commands.MakeDecision;
using Maintenance.Application.Features.Commands.RejectAssignment;
using Maintenance.Application.Features.Queries.GetVendorTickets;
using Maintenance.Application.Features.Queries.GetVendorDashboard;
using Maintenance.Application.Features.Queries.GetAdminTickets;
using Maintenance.Application.Features.Commands.RaiseDispute;
using Maintenance.Application.Features.Commands.ResolveDispute;
using Maintenance.Application.Features.Commands.StartWork;
using Maintenance.Application.Features.Commands.SubmitTicket;
using Maintenance.Application.Features.Queries.GetDisputeDetails;
using Maintenance.Application.Features.Commands.UpdateTicket;
using Maintenance.Application.Features.Commands.UploadAttachment;
using Maintenance.Application.Features.Queries.GetAiEvaluation;
using Maintenance.Application.Features.Queries.GetPayment;
using Maintenance.Application.Features.Queries.GetTicketById;
using Maintenance.Application.Features.Queries.GetTickets;
using Maintenance.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Identity.Application.Interfaces;

namespace RealEstate.Host.Controllers;

[ApiController]
[Route("api/maintenance/tickets")]
[Authorize]
public class TicketsController(ISender sender, ICurrentUserService currentUser) : ControllerBase
{
    // ── helpers ────────────────────────────────────────────────────
    private Guid CurrentUserId => currentUser.UserId
        ?? throw new UnauthorizedAccessException("User ID not found in token.");

    // ── Admin ──────────────────────────────────────────────────────

    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAllForAdmin(
        [FromServices] IUserRepository userRepo,
        [FromServices] IMaintenanceCompanyService companySvc,
        [FromQuery] TicketStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetAdminTicketsQuery(status, page, pageSize), ct);

        // Batch-fetch tenant and owner names in a single DB round-trip
        var userIds = result.Items
            .SelectMany(t => new[] { t.TenantId, t.OwnerId })
            .Distinct();
        var nameMap = await userRepo.GetFullNamesByIdsAsync(userIds, ct);

        // Batch-fetch all company names (one call, then filter in-memory)
        var allCompanies = await companySvc.GetAllAsync(null, null, null);
        var companyNameMap = allCompanies.ToDictionary(c => c.Id, c => c.CompanyName);

        var enriched = result.Items
            .Select(t => t with
            {
                TenantName = nameMap.GetValueOrDefault(t.TenantId),
                OwnerName  = nameMap.GetValueOrDefault(t.OwnerId),
                VendorCompanyName = t.VendorAssignment is not null
                    ? companyNameMap.GetValueOrDefault(t.VendorAssignment.VendorId)
                    : null
            })
            .ToList();

        return Ok(result with { Items = enriched });
    }

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
        await sender.Send(new MakeDecisionCommand(id, req.ResolutionMethod, CurrentUserId));
        return Ok(new { message = $"Decision recorded: {req.ResolutionMethod}" });
    }

    // ── Vendor Assignment ──────────────────────────────────────────
    [HttpGet("my-assignments")]
    [Authorize(Roles = "maintenance_company")]
    public async Task<IActionResult> GetMyAssignments(
        [FromServices] IMaintenanceCompanyService companySvc, CancellationToken ct)
    {
        // VendorId in maintenance_ticket_vendor_assignments stores maintenanceComp.MaintenanceCompanies.Id,
        // because callers discover vendor IDs from the company profile/creation API (maintenanceComp schema).
        // We must look up that same Id by translating: JWT UserId → maintenanceComp.UserId (string) → company.Id.
        var company = await companySvc.GetByUserIdAsync(CurrentUserId.ToString())
            ?? throw new KeyNotFoundException($"Maintenance company profile not found for UserId={CurrentUserId}.");

        var result = await sender.Send(new GetVendorTicketsQuery(company.Id));
        return Ok(result);
    }

    [HttpGet("vendor-dashboard")]
    [Authorize(Roles = "maintenance_company")]
    public async Task<IActionResult> GetVendorDashboard(
        [FromServices] IMaintenanceCompanyService companySvc, CancellationToken ct)
    {
        var company = await companySvc.GetByUserIdAsync(CurrentUserId.ToString())
            ?? throw new KeyNotFoundException($"Maintenance company profile not found for UserId={CurrentUserId}.");

        var result = await sender.Send(new GetVendorDashboardQuery(company.Id));
        return Ok(result);
    }

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

    // User marks ticket complete when they hired an external technician themselves
    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = "tenant,owner")]
    public async Task<IActionResult> MarkCompleted(Guid id)
    {
        await sender.Send(new MarkCompletedCommand(id));
        return Ok(new { message = "Ticket marked as completed." });
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

    // Vendor sets the price — creates a pending payment record
    [HttpPost("{id:guid}/payments")]
    [Authorize(Roles = "maintenance_company")]
    public async Task<IActionResult> CreatePayment(Guid id, [FromBody] CreatePaymentRequest req)
    {
        var paymentId = await sender.Send(new CreatePaymentCommand(
            TicketId: id,
            VendorId: CurrentUserId,
            Amount: req.Amount,
            Currency: req.Currency));

        return Ok(new { paymentId });
    }

    // User pays online — submits transaction ID from the website payment
    [HttpPost("payments/{paymentId:guid}/pay")]
    [Authorize(Roles = "tenant,owner")]
    public async Task<IActionResult> PayOnline(Guid paymentId, [FromBody] PayOnlineRequest req)
    {
        await sender.Send(new UserPayCommand(paymentId, req.TransactionId));
        return Ok(new { message = "Payment submitted. Awaiting vendor confirmation." });
    }

    // Vendor confirms the payment after seeing it — closes the ticket
    [HttpPost("payments/{paymentId:guid}/confirm")]
    [Authorize(Roles = "maintenance_company")]
    public async Task<IActionResult> ConfirmPayment(Guid paymentId)
    {
        await sender.Send(new ConfirmPaymentCommand(paymentId));
        return Ok(new { message = "Payment confirmed. Ticket marked as Completed." });
    }

    [HttpGet("{id:guid}/payments")]
    public async Task<IActionResult> GetPayment(Guid id)
    {
        var result = await sender.Send(new GetPaymentQuery(id));
        return result is null ? NotFound() : Ok(result);
    }

    // Creates (or returns cached) Stripe Checkout URL for a maintenance payment.
    // company_manager is included because the company-facing ticket has OwnerId = company manager.
    [HttpPost("{id:guid}/payments/checkout")]
    [Authorize(Roles = "tenant,owner,company_manager")]
    public async Task<IActionResult> CreateCheckout(Guid id)
    {
        var checkoutUrl = await sender.Send(new CreateMaintenanceCheckoutCommand(id));
        return Ok(new { checkoutUrl });
    }

    // Company manager sees tickets auto-routed to their company.
    // Admins can query any company; company_manager can only query their own.
    [HttpGet("company/{companyId:guid}/routed")]
    [Authorize(Policy = "CompanyManager")]
    public async Task<IActionResult> GetCompanyRoutedTickets(Guid companyId)
    {
        if (currentUser.IsInRole("company_manager") && currentUser.CompanyId != companyId)
            return Forbid();

        var result = await sender.Send(new GetCompanyRoutedTicketsQuery(companyId));
        return Ok(result);
    }

    [HttpPost("{id:guid}/start-work")]
    [Authorize(Roles = "maintenance_company")]
    public async Task<IActionResult> StartWork(Guid id)
    {
        await sender.Send(new StartWorkCommand(id));
        return Ok(new { message = "Work started. Status is now InProgress." });
    }

    // ── Ticket Dispute ─────────────────────────────────────────────

    /// <summary>Tenant, Owner, or Company Manager disputes the AI's responsibility decision.</summary>
    [HttpPost("{id:guid}/dispute")]
    [Authorize(Roles = "tenant,owner,company_manager")]
    public async Task<IActionResult> RaiseDispute(Guid id, [FromBody] RaiseDisputeRequest req)
    {
        var role = currentUser.Role ?? "unknown";
        await sender.Send(new RaiseTicketDisputeCommand(id, CurrentUserId, role, req.Reason));
        return Ok(new { message = "Dispute raised. An admin will review the ticket." });
    }

    /// <summary>Admin sets the final responsible party, overriding the AI decision.</summary>
    [HttpPost("{id:guid}/resolve-dispute")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ResolveDispute(Guid id, [FromBody] ResolveDisputeRequest req)
    {
        await sender.Send(new ResolveTicketDisputeCommand(id, req.FinalResponsibleParty, req.AdminNotes));
        return Ok(new { message = $"Dispute resolved. Final responsible party: {req.FinalResponsibleParty}." });
    }

    /// <summary>Admin-only: returns the full dispute context including AI evaluation, images, and lease contract.</summary>
    [HttpGet("{id:guid}/dispute-details")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetDisputeDetails(Guid id)
    {
        var result = await sender.Send(new GetDisputeDetailsQuery(id));
        return Ok(result);
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
public record CreatePaymentRequest(decimal Amount, string Currency = "EGP");
public record PayOnlineRequest(string TransactionId);
public record RaiseDisputeRequest(string Reason);
public record ResolveDisputeRequest(string FinalResponsibleParty, string? AdminNotes);