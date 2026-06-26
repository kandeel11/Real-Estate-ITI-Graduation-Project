using Document.Application.Commands.Lease.ConfirmLease;
using Document.Application.Commands.Lease.TenantAcceptLease;
using Document.Application.Commands.Lease.RequestRenewal;
using Document.Application.Queries.Lead.GetMyLeads;
using Document.Application.Queries.Lead.GetTenantNoManagementRented;
using Document.Application.Queries.Lease.GetLeaseById;
using Document.Application.Queries.Lease;
using Document.Application.Queries.Lease.GetLeasePaymentSchedule;
using Identity.Application.Commands.ChangePassword;
using Identity.Application.Commands.UpdateProfile;
using Identity.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payment.Application.Features.Queries.GetMyRentPayments;
using Property.Application.Features.Queries.GetPropertyById;
using RealEstate.Host.Services;
using Identity.Application.Queries.GetCurrentUser;

namespace RealEstate.Host.Controllers;

[ApiController]
[Route("api/tenant")]
[Authorize(Roles = "tenant")]
public class TenantController(
    ISender sender,
    ICurrentUserService currentUser,
    PropertyEnrichmentService enrichment) : ControllerBase
{
    private Guid UserId => currentUser.UserId ?? throw new UnauthorizedAccessException();

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var user = await sender.Send(new GetCurrentUserQuery(), ct);
        if (user is null) return NotFound();

        return Ok(new
        {
            id = user.Id,
            fullName = user.FullName,
            email = user.Email,
            phone = user.Phone,
            role = user.Role,
            isEmailVerified = user.IsEmailVerified,
            isActive = user.IsActive
        });
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest req, CancellationToken ct)
    {
        await sender.Send(new UpdateProfileCommand(UserId, req.FullName, req.Phone, req.Email), ct);
        return NoContent();
    }

    [HttpPut("profile/password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req, CancellationToken ct)
    {
        if (req.NewPassword != req.ConfirmNewPassword)
            return BadRequest(new { error = "New password and confirmation do not match." });
        try
        {
            await sender.Send(new ChangePasswordCommand(UserId, req.CurrentPassword, req.NewPassword), ct);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("rented-properties")]
    public async Task<IActionResult> GetRentedProperties(CancellationToken ct)
    {
        var leases = await sender.Send(new GetMyLeasesQuery(UserId), ct);
        var result = new List<object>();

        // Lease-based rented properties (full-management or standard path)
        var leasePropertyIds = leases.Select(l => l.PropertyId).Distinct().ToList();
        foreach (var pid in leasePropertyIds)
        {
            var prop = await sender.Send(new GetPropertyByIdQuery(pid), ct);
            if (prop is null) continue;
            await enrichment.EnrichAsync(prop, ct);

            var lease = leases
                .Where(l => l.PropertyId == pid)
                .OrderByDescending(l => l.IsActive)
                .ThenByDescending(l => l.StartDate)
                .First();

            var daysLeft = lease.EndDate.HasValue
                ? (int)(lease.EndDate.Value.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow.Date).TotalDays
                : (int?)null;

            result.Add(new
            {
                property = prop,
                leaseId = lease.Id,
                installmentAmount = lease.InstallmentAmount,
                startDate = lease.StartDate,
                endDate = lease.EndDate,
                isActive = lease.IsActive,
                daysUntilExpiry = daysLeft,
                noManagement = false
            });
        }

        // No-management rented properties (no lease document, deal closed directly)
        var noMgmtPropertyIds = await sender.Send(new GetTenantNoManagementRentedQuery(UserId), ct);
        foreach (var pid in noMgmtPropertyIds.Where(pid => !leasePropertyIds.Contains(pid)))
        {
            var prop = await sender.Send(new GetPropertyByIdQuery(pid), ct);
            if (prop is null) continue;
            await enrichment.EnrichAsync(prop, ct);

            result.Add(new
            {
                property = prop,
                leaseId = (Guid?)null,
                installmentAmount = (decimal?)null,
                startDate = (DateOnly?)null,
                endDate = (DateOnly?)null,
                isActive = true,
                daysUntilExpiry = (int?)null,
                noManagement = true
            });
        }

        return Ok(result);
    }

    [HttpGet("leases")]
    public async Task<IActionResult> GetLeases(CancellationToken ct)
        => Ok(await sender.Send(new GetMyLeasesQuery(UserId), ct));

    [HttpGet("leases/active")]
    public async Task<IActionResult> GetActiveLease(CancellationToken ct)
    {
        var lease = await sender.Send(new GetActiveLeaseQuery(UserId), ct);
        return lease is null ? NotFound(new { message = "No active lease found." }) : Ok(lease);
    }

    [HttpGet("leases/pending-offer")]
    public async Task<IActionResult> GetPendingLeaseOffer(CancellationToken ct)
    {
        var lease = await sender.Send(new GetPendingLeaseOfferQuery(UserId), ct);
        if (lease is null) return NotFound(new { message = "No pending lease offer." });

        var prop = await sender.Send(new GetPropertyByIdQuery(lease.PropertyId), ct);

        return Ok(new
        {
            id            = lease.Id,
            propertyId    = lease.PropertyId,
            propertyTitle = prop?.Title ?? $"Property {lease.PropertyId.ToString()[..6]}",
            startDate     = lease.StartDate,
            endDate       = lease.EndDate,
            installmentAmount = lease.InstallmentAmount,
            paymentFrequency  = lease.PaymentFrequency,
            totalRentAmount   = lease.TotalRentAmount,
            deposit       = lease.SecurityDeposit,
            pdfUrl        = lease.ContractFileUrl,
            status        = "pending_tenant",
            sentAt        = lease.CreatedAt
        });
    }

    [HttpPost("leases/{id}/accept")]
    public async Task<IActionResult> AcceptLeaseOffer(Guid id, CancellationToken ct)
    {
        try
        {
            await sender.Send(new TenantAcceptLeaseCommand(id, UserId), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)      { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { error = ex.Message }); }
        catch (InvalidOperationException ex)  { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("payments")]
    public async Task<IActionResult> GetMyPayments(CancellationToken ct)
    {
        var result = await sender.Send(new GetMyRentPaymentsQuery(UserId, "tenant"), ct);
        return Ok(result);
    }

    [HttpGet("payments/schedule")]
    public async Task<IActionResult> GetPaymentSchedule(CancellationToken ct)
    {
        var lease = await sender.Send(new GetActiveLeaseQuery(UserId), ct);
        if (lease is null) return NotFound(new { message = "No active lease found." });

        var schedule = await sender.Send(new GetLeasePaymentScheduleQuery(lease.Id), ct);
        if (schedule is null)
            return UnprocessableEntity(new { error = "Payment schedule not available — lease extraction incomplete." });

        return Ok(schedule);
    }

    [HttpPost("leases/{id}/decline")]
    public async Task<IActionResult> DeclineLeaseOffer(Guid id, [FromBody] DeclineLeaseRequest req, CancellationToken ct)
    {
        var lease = await sender.Send(new GetLeaseByIdQuery(id), ct);
        if (lease is null) return NotFound(new { message = "Lease not found." });
        if (lease.TenantId != UserId) return Forbid();

        var result = await sender.Send(
            new RejectLeaseCommand(id, UserId, req.Reason ?? "Declined by tenant"), ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns the tenant's full lead pipeline — all leads that originated from their
    /// inquiries, ordered by last update, with the pipeline stage clearly shown.
    /// </summary>
    [HttpGet("lead-pipeline")]
    public async Task<IActionResult> GetLeadPipeline(CancellationToken ct)
        => Ok(await sender.Send(new GetMyLeadsQuery(UserId), ct));

    /// <summary>Tenant asks the landlord to renew an active lease nearing its end.</summary>
    [HttpPost("leases/{id}/renew-request")]
    public async Task<IActionResult> RequestRenewal(Guid id, CancellationToken ct)
    {
        try
        {
            await sender.Send(new RequestLeaseRenewalCommand(id, UserId), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }
}

public record UpdateProfileRequest(string FullName, string Phone, string? Email);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmNewPassword);
public record DeclineLeaseRequest(string? Reason);