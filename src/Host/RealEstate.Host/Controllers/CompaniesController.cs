using Document.Application.Commands.Lead.ManagerConfirmNoManagement;
using Document.Application.Queries.Inquiry;
using Document.Application.Queries.Lead.GetBrokerLeadPipeline;
using Document.Application.Queries.Lead.GetNoManagementPendingLeads;
using Document.Application.Queries.Lease;
using Document.Application.Queries.Lease.GetLeasePaymentSchedule;
using Document.Application.Queries.Rating;
using Payment.Application.Features.Queries.GetRentPaymentsByLeaseIds;
using Identity.Application.Commands.UpdateCompany;
using Identity.Application.Commands.UpdateCompanyLogo;
using Identity.Application.Commands.VerifyCompany;
using Identity.Application.Interfaces;
using Identity.Application.Queries.GetAllCompanies;
using Identity.Application.Queries.GetCompanyById;
using Identity.Application.Queries.GetCompanyBrokersDetails;
using Identity.Application.Queries.GetCompanyDetails;
using Identity.Application.Queries.GetUserNames;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Property.Application.Features.Queries.GetPropertiesByCompany;
using Property.Application.Interfaces;
using RealEstate.Host.Services;
using static Property.Application.Interfaces.IMediaStorageService;

namespace RealEstate.Host.Controllers;

[ApiController]
[Route("api/companies")]
public class CompaniesController(
    IMediator mediator,
    ICurrentUserService currentUser,
    PropertyEnrichmentService enrichment,
    IMediaStorageService mediaStorage) : ControllerBase
{
    /// <summary>
    /// Lists all registered companies. Admin only.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var companies = await mediator.Send(new GetAllCompaniesQuery(), ct);
        return Ok(companies);
    }

    /// <summary>
    /// Uploads the company logo and persists its URL on the company profile.
    /// Returns the stored URL so the client can update its preview.
    /// </summary>
    [HttpPost("mine/logo")]
    [Authorize(Policy = "CompanyManager")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5 * 1024 * 1024)] // 5 MB cap for logos
    public async Task<IActionResult> UploadLogo([FromForm] UploadLogoRequest request, CancellationToken ct)
    {
        var companyId = currentUser.CompanyId;
        if (companyId is null)
            return BadRequest(new { error = "No company is associated with your account." });

        if (request.File is null || request.File.Length == 0)
            return BadRequest(new { error = "No file provided." });

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/svg+xml" };
        if (!allowedTypes.Contains(request.File.ContentType))
            return BadRequest(new { error = "Logo must be a JPEG, PNG, WebP, or SVG image." });

        await using var stream = request.File.OpenReadStream();
        var logoUrl = await mediaStorage.UploadAsync(
            OwnerId: currentUser.UserId,
            fileStream: stream,
            fileName: request.File.FileName,
            contentType: request.File.ContentType,
            category: MediaCategory.Photo,
            ct: ct);

        await mediator.Send(new UpdateCompanyLogoCommand(companyId.Value, logoUrl), ct);

        return Ok(new { logoUrl });
    }

    /// <summary>
    /// Returns the company belonging to the logged-in company manager (reads CompanyId from JWT).
    /// Uses CompanyDetailsDto so branding fields (logo/website/description) round-trip.
    /// </summary>
    [HttpGet("mine")]
    [Authorize(Policy = "CompanyManager")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var companyId = currentUser.CompanyId;
        if (companyId is null)
            return BadRequest(new { error = "No company is associated with your account." });

        var company = await mediator.Send(new GetCompanyDetailsQuery(companyId.Value), ct);
        return company is null ? NotFound() : Ok(company);
    }

    /// <summary>
    /// Returns a company by ID. Accessible to admins or the company's own manager.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var company = await mediator.Send(new GetCompanyByIdQuery(id), ct);
        return company is null ? NotFound() : Ok(company);
    }

    /// <summary>
    /// Updates the logged-in manager's OWN company profile (companyId from JWT).
    /// </summary>
    [HttpPut("mine")]
    [Authorize(Policy = "CompanyManager")]
    public async Task<IActionResult> UpdateMine([FromBody] UpdateCompanyRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId is null) return Unauthorized(new { error = "User not authenticated." });

        if (!currentUser.IsEmailVerified)
            return StatusCode(StatusCodes.Status403Forbidden,
                new { error = "Please verify your email to update your company profile." });

        var companyId = currentUser.CompanyId;
        if (companyId is null) return BadRequest(new { error = "No company is associated with your account." });

        try
        {
            await mediator.Send(new UpdateCompanyCommand(
                CompanyId: companyId.Value,
                RequestingUserId: userId.Value,
                Name: request.Name,
                City: request.City,
                Address: request.Address,
                Phone: request.Phone,
                Website: request.Website,
                Description: request.Description,
                LogoUrl: request.LogoUrl), ct);

            return NoContent();
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    /// <summary>
    /// Updates company profile. Only the company's manager or an admin can call this.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CompanyManager")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCompanyRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId is null) return Unauthorized(new { error = "User not authenticated." });

        // Verify caller manages this company (unless admin)
        if (!currentUser.IsInRole("admin") && currentUser.CompanyId != id)
            return Forbid();

        try
        {
            await mediator.Send(new UpdateCompanyCommand(
                CompanyId: id,
                RequestingUserId: userId.Value,
                Name: request.Name,
                City: request.City,
                Address: request.Address,
                Phone: request.Phone,
                Website: request.Website,
                Description: request.Description,
                LogoUrl: request.LogoUrl), ct);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// Marks a company as verified. Admin only.
    /// </summary>
    [HttpPut("{id:guid}/verify")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Verify(Guid id, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new VerifyCompanyCommand(id), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Lists all properties belonging to the logged-in manager's OWN company (companyId from JWT).
    /// </summary>
    [HttpGet("mine/properties")]
    [Authorize(Policy = "CompanyManager")]
    public async Task<IActionResult> GetMyProperties(CancellationToken ct)
    {
        var companyId = currentUser.CompanyId;
        if (companyId is null)
            return BadRequest(new { error = "No company is associated with your account." });

        var properties = await mediator.Send(new GetPropertiesByCompanyQuery(companyId.Value), ct);
        await enrichment.EnrichAsync(properties, ct);
        return Ok(properties);
    }

    /// <summary>
    /// Lists all brokers belonging to the logged-in manager's OWN company (companyId from JWT).
    /// Uses BrokerDetailsDto: includes the broker's name/contact and performance counters.
    /// </summary>
    [HttpGet("mine/brokers")]
    [Authorize(Policy = "CompanyManager")]
    public async Task<IActionResult> GetMyBrokers(CancellationToken ct)
    {
        var companyId = currentUser.CompanyId;
        if (companyId is null)
            return BadRequest(new { error = "No company is associated with your account." });

        var brokers = await mediator.Send(new GetCompanyBrokersDetailsQuery(companyId.Value), ct);
        return Ok(brokers);
    }

    /// <summary>
    /// Dashboard stat counters for the manager's own company, composed from the
    /// Property, Identity, and Document modules (no cross-module references —
    /// composition happens here in the Host, like PropertyEnrichmentService).
    /// </summary>
    [HttpGet("mine/stats")]
    [Authorize(Policy = "CompanyManager")]
    public async Task<IActionResult> GetMyStats(CancellationToken ct)
    {
        var companyId = currentUser.CompanyId;
        if (companyId is null)
            return BadRequest(new { error = "No company is associated with your account." });

        var properties = await mediator.Send(new GetPropertiesByCompanyQuery(companyId.Value), ct);
        var brokers = await mediator.Send(new GetCompanyBrokersDetailsQuery(companyId.Value), ct);
        var inquiries = await mediator.Send(new GetCompanyInquiryStatsQuery(companyId.Value, 0), ct);

        return Ok(new CompanyStatsResponse(
            TotalProperties: properties.Count,
            ActiveProperties: properties.Count(p => p.Status == "Active"),
            PendingProperties: properties.Count(p => p.Status == "PendingReview"),
            RentedProperties: properties.Count(p => p.Status == "Rented"),
            TotalBrokers: brokers.Count(b => b.IsActive),
            TotalViews: properties.Sum(p => p.ViewsCount),
            TotalInquiries: inquiries.TotalInquiries,
            MonthlyInquiries: inquiries.MonthlyInquiries,
            DealsClosed: brokers.Sum(b => b.ClosedDealsCount)));
    }

    /// <summary>
    /// Star-rating distribution across the company's ratings (by CompanyId or
    /// any of its properties). Feeds the Performance page's ratings card.
    /// </summary>
    [HttpGet("mine/ratings-summary")]
    [Authorize(Policy = "CompanyManager")]
    public async Task<IActionResult> GetMyRatingsSummary(CancellationToken ct)
    {
        var companyId = currentUser.CompanyId;
        if (companyId is null)
            return BadRequest(new { error = "No company is associated with your account." });

        var properties = await mediator.Send(new GetPropertiesByCompanyQuery(companyId.Value), ct);
        var summary = await mediator.Send(new GetCompanyRatingSummaryQuery(
            companyId.Value, properties.Select(p => p.Id).ToList()), ct);

        return Ok(summary);
    }

    /// <summary>
    /// Full inquiry history for the manager's own company, enriched with the
    /// tenant (user) name, the broker who handled it, and the property title.
    /// </summary>
    [HttpGet("mine/inquiries")]
    [Authorize(Policy = "CompanyManager")]
    public async Task<IActionResult> GetMyInquiries(CancellationToken ct)
    {
        var companyId = currentUser.CompanyId;
        if (companyId is null)
            return BadRequest(new { error = "No company is associated with your account." });

        var inquiries = await mediator.Send(new GetCompanyInquiryHistoryQuery(companyId.Value), ct);
        var properties = await mediator.Send(new GetPropertiesByCompanyQuery(companyId.Value), ct);
        var titlesById = properties.ToDictionary(p => p.Id, p => p.Title);

        var userIds = inquiries.Select(i => i.TenantId)
            .Concat(inquiries.Where(i => i.AcceptedById is not null).Select(i => i.AcceptedById!.Value))
            .Distinct()
            .ToList();
        var names = await mediator.Send(new GetUserNamesQuery(userIds), ct);

        var result = inquiries.Select(i => new CompanyInquiryResponse(
            i.Id,
            i.PropertyId,
            titlesById.TryGetValue(i.PropertyId, out var title) ? title : "—",
            i.TenantId,
            names.TryGetValue(i.TenantId, out var tenant) ? tenant : "Unknown user",
            i.AcceptedById,
            i.AcceptedById is not null && names.TryGetValue(i.AcceptedById.Value, out var broker) ? broker : null,
            i.Status,
            i.Message,
            i.CreatedAt,
            i.AcceptedAt,
            i.ClosedAt));

        return Ok(result);
    }

    /// <summary>
    /// All rent payments for the company's properties (across all brokers and leases).
    /// Returns RentPaymentDto[] enriched with propertyId/tenantId/landlordId.
    /// </summary>
    [HttpGet("mine/payments")]
    [Authorize(Policy = "CompanyManager")]
    public async Task<IActionResult> GetMyPayments(CancellationToken ct)
    {
        var companyId = currentUser.CompanyId;
        if (companyId is null)
            return BadRequest(new { error = "No company is associated with your account." });

        var properties = await mediator.Send(new GetPropertiesByCompanyQuery(companyId.Value), ct);
        if (properties.Count == 0) return Ok(Array.Empty<object>());

        var propertyIds = properties.Select(p => p.Id).ToList();
        var leases = await mediator.Send(new GetLeasesByPropertyIdsQuery(propertyIds), ct);
        if (leases.Count == 0) return Ok(Array.Empty<object>());

        var leaseIds = leases.Select(l => l.Id).ToList();
        var payments = await mediator.Send(new GetRentPaymentsByLeaseIdsQuery(leaseIds), ct);
        return Ok(payments);
    }

    /// <summary>
    /// Payment schedule for every active lease under the company's properties.
    /// Returns one schedule entry per lease enriched with property title and tenant ID.
    /// </summary>
    [HttpGet("mine/payments/schedule")]
    [Authorize(Policy = "CompanyManager")]
    public async Task<IActionResult> GetMyPaymentsSchedule(CancellationToken ct)
    {
        var companyId = currentUser.CompanyId;
        if (companyId is null)
            return BadRequest(new { error = "No company is associated with your account." });

        var properties = await mediator.Send(new GetPropertiesByCompanyQuery(companyId.Value), ct);
        if (properties.Count == 0) return Ok(Array.Empty<object>());

        var propertyIds = properties.Select(p => p.Id).ToList();
        var leases = await mediator.Send(new GetLeasesByPropertyIdsQuery(propertyIds), ct);
        var activeLeases = leases.Where(l => l.IsActive).ToList();
        if (activeLeases.Count == 0) return Ok(Array.Empty<object>());

        var propertyTitles = properties.ToDictionary(p => p.Id, p => p.Title);

        var result = new List<object>(activeLeases.Count);
        foreach (var lease in activeLeases)
        {
            var schedule = await mediator.Send(new GetLeasePaymentScheduleQuery(lease.Id), ct);
            result.Add(new
            {
                leaseId = lease.Id,
                propertyId = lease.PropertyId,
                propertyTitle = propertyTitles.GetValueOrDefault(lease.PropertyId, string.Empty),
                tenantId = lease.TenantId,
                startDate = lease.StartDate,
                endDate = lease.EndDate,
                schedule
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Recent activity feed for the manager's own company, synthesized from
    /// real events: listings created, brokers joining, inquiries received.
    /// </summary>
    [HttpGet("mine/activity")]
    [Authorize(Policy = "CompanyManager")]
    public async Task<IActionResult> GetMyActivity([FromQuery] int take = 10, CancellationToken ct = default)
    {
        var companyId = currentUser.CompanyId;
        if (companyId is null)
            return BadRequest(new { error = "No company is associated with your account." });

        var properties = await mediator.Send(new GetPropertiesByCompanyQuery(companyId.Value), ct);
        var brokers = await mediator.Send(new GetCompanyBrokersDetailsQuery(companyId.Value), ct);
        var inquiries = await mediator.Send(new GetCompanyInquiryStatsQuery(companyId.Value, take), ct);

        var titlesById = properties.ToDictionary(p => p.Id, p => p.Title);

        var items = new List<CompanyActivityItem>();

        items.AddRange(properties.Select(p => new CompanyActivityItem(
            "listing_created", $"Listing \"{p.Title}\" created", p.CreatedAt)));

        items.AddRange(brokers.Select(b => new CompanyActivityItem(
            "broker_joined", $"Broker {(string.IsNullOrEmpty(b.FullName) ? b.LicenseNumber : b.FullName)} joined the team", b.CreatedAt)));

        items.AddRange(inquiries.Recent.Select(i => new CompanyActivityItem(
            "inquiry_received",
            titlesById.TryGetValue(i.PropertyId, out var title)
                ? $"New inquiry received on \"{title}\""
                : "New inquiry received",
            i.CreatedAt)));

        return Ok(items.OrderByDescending(i => i.Timestamp).Take(take).ToList());
    }

    /// <summary>
    /// Lists all properties belonging to a company (created by any of its brokers or the manager).
    /// </summary>
    [HttpGet("{id:guid}/properties")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProperties(Guid id, CancellationToken ct)
    {
        var properties = await mediator.Send(new GetPropertiesByCompanyQuery(id), ct);
        await enrichment.EnrichAsync(properties, ct);
        return Ok(properties);
    }

    /// <summary>
    /// Returns per-broker lead pipeline for the company manager's own company.
    /// Shape is optimized for Kanban board rendering — each broker has a column
    /// for every LeadStatus, plus summary counters.
    /// </summary>
    [HttpGet("mine/lead-pipeline")]
    [Authorize(Policy = "CompanyManager")]
    public async Task<IActionResult> GetLeadPipeline(CancellationToken ct)
    {
        var companyId = currentUser.CompanyId;
        if (companyId is null)
            return BadRequest(new { error = "No company is associated with your account." });

        var brokers = await mediator.Send(new GetCompanyBrokersDetailsQuery(companyId.Value), ct);

        var company = await mediator.Send(
            new Identity.Application.Queries.GetCompanyDetails.GetCompanyDetailsQuery(companyId.Value), ct);
        var companyName = company?.Name ?? string.Empty;

        if (brokers.Count == 0)
            return Ok(new
            {
                companyId,
                companyName,
                totalBrokers = 0,
                brokers = Array.Empty<object>()
            });

        var brokerUserIds = brokers.Select(b => b.UserId).ToList();
        var pipelines = await mediator.Send(new GetBrokerLeadPipelineQuery(brokerUserIds), ct);

        var brokerNameById = brokers.ToDictionary(b => b.UserId, b => b.FullName);

        var result = pipelines.Select(p => new
        {
            brokerId     = p.BrokerUserId,
            brokerUserId = p.BrokerUserId,
            brokerName   = brokerNameById.GetValueOrDefault(p.BrokerUserId, "Unknown"),
            totalLeads   = p.TotalLeads,
            closedWon    = p.ClosedWon,
            pipeline     = p.Kanban        // renamed: frontend reads "pipeline", not "kanban"
        });

        return Ok(new
        {
            companyId,
            companyName,
            totalBrokers = brokers.Count,
            brokers      = result
        });
    }

    /// <summary>
    /// Lists all brokers belonging to a company.
    /// </summary>
    [HttpGet("{id:guid}/brokers")]
    [Authorize(Policy = "CompanyManager")]
    public async Task<IActionResult> GetBrokers(Guid id, CancellationToken ct)
    {
        // Company manager can only see their own company's brokers
        if (!currentUser.IsInRole("admin") && currentUser.CompanyId != id)
            return Forbid();

        // Use the enriched details query so callers get FullName/Email/ClosedDealsCount
        // (the bare BrokerDto has none of those → admin UI showed "Unknown" / no deals).
        var brokers = await mediator.Send(new GetCompanyBrokersDetailsQuery(id), ct);
        return Ok(brokers);
    }

    /// <summary>
    /// Leases across the company's properties that are waiting for the manager to approve
    /// the broker's management-option selection (ManagerApprovalStatus = "PendingApproval").
    /// </summary>
    [HttpGet("mine/leases/pending-approval")]
    [Authorize(Policy = "CompanyManager")]
    public async Task<IActionResult> GetPendingApprovalLeases(CancellationToken ct)
    {
        var companyId = currentUser.CompanyId;
        if (companyId is null)
            return BadRequest(new { error = "No company is associated with your account." });

        var properties = await mediator.Send(new GetPropertiesByCompanyQuery(companyId.Value), ct);
        if (properties.Count == 0) return Ok(Array.Empty<object>());

        var propertyIds = properties.Select(p => p.Id).ToList();
        var leases = await mediator.Send(new GetPendingApprovalLeasesQuery(propertyIds), ct);
        if (leases.Count == 0) return Ok(Array.Empty<object>());

        var userIds = leases.Select(l => l.TenantId)
            .Concat(leases.Where(l => l.BrokerId.HasValue).Select(l => l.BrokerId!.Value))
            .Distinct().ToList();
        var names = await mediator.Send(new GetUserNamesQuery(userIds), ct);
        var titlesById = properties.ToDictionary(p => p.Id, p => p.Title);

        var result = leases.Select(l => new
        {
            id                   = l.Id,
            propertyId           = l.PropertyId,
            propertyTitle        = titlesById.GetValueOrDefault(l.PropertyId, string.Empty),
            tenantId             = l.TenantId,
            tenantName           = names.GetValueOrDefault(l.TenantId, "Unknown"),
            brokerId             = l.BrokerId,
            brokerName           = l.BrokerId.HasValue ? names.GetValueOrDefault(l.BrokerId.Value) : null,
            installmentAmount    = l.InstallmentAmount,
            paymentFrequency     = l.PaymentFrequency,
            totalRentAmount      = l.TotalRentAmount,
            durationInMonths     = l.DurationInMonths,
            securityDeposit      = l.SecurityDeposit,
            startDate            = l.StartDate,
            endDate              = l.EndDate,
            paymentDayOfMonth    = l.PaymentDayOfMonth,
            managerApprovalStatus = l.ManagerApprovalStatus,
            managerNotes         = l.ManagerNotes,
            isFullManagement     = l.IsFullManagement,
            managementOptions    = ParseManagementOptions(l.ManagementOptions),
            uploadedAt           = l.CreatedAt,
            contractFileUrl      = l.ContractFileUrl
        });

        return Ok(result);
    }

    /// <summary>
    /// Deals closed by company brokers without management, waiting for manager confirmation.
    /// After confirming, the property is marked Rented and the tenant is notified.
    /// </summary>
    [HttpGet("mine/leads/no-management-pending")]
    [Authorize(Policy = "CompanyManager")]
    public async Task<IActionResult> GetNoManagementPending(CancellationToken ct)
    {
        var managerId = currentUser.UserId;
        if (managerId is null)
            return Unauthorized();

        var items = await mediator.Send(
            new GetNoManagementPendingLeadsQuery(managerId.Value), ct);

        if (items.Count == 0) return Ok(Array.Empty<object>());

        var userIds = items.Select(i => i.TenantId).Distinct().ToList();
        var names = await mediator.Send(new GetUserNamesQuery(userIds), ct);

        var result = items.Select(i => new
        {
            leadId      = i.LeadId,
            propertyId  = i.PropertyId,
            tenantId    = i.TenantId,
            tenantName  = names.GetValueOrDefault(i.TenantId, "Unknown"),
            dealValue   = i.DealValue,
            pendingAt   = i.PendingAt
        });

        return Ok(result);
    }

    /// <summary>
    /// Company manager confirms a broker's "no management" deal close.
    /// Marks the property as Rented and notifies the tenant.
    /// </summary>
    [HttpPost("mine/leads/{leadId}/confirm-no-management")]
    [Authorize(Policy = "CompanyManager")]
    public async Task<IActionResult> ConfirmNoManagement(Guid leadId, CancellationToken ct)
    {
        var managerId = currentUser.UserId;
        if (managerId is null) return Unauthorized();
        try
        {
            await mediator.Send(
                new ManagerConfirmNoManagementCommand(leadId, managerId.Value), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)       { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex)  { return BadRequest(new { error = ex.Message }); }
        catch (UnauthorizedAccessException ex){ return StatusCode(403, new { error = ex.Message }); }
    }

    /// <summary>
    /// Leases across the company's properties that the manager has approved and forwarded
    /// to the tenant — now waiting for the tenant to call tenant-accept
    /// (ManagerApprovalStatus = "Approved", ExtractionStatus = "extracted").
    /// </summary>
    [HttpGet("mine/leases/awaiting-tenant")]
    [Authorize(Policy = "CompanyManager")]
    public async Task<IActionResult> GetAwaitingTenantLeases(CancellationToken ct)
    {
        var companyId = currentUser.CompanyId;
        if (companyId is null)
            return BadRequest(new { error = "No company is associated with your account." });

        var properties = await mediator.Send(new GetPropertiesByCompanyQuery(companyId.Value), ct);
        if (properties.Count == 0) return Ok(Array.Empty<object>());

        var propertyIds = properties.Select(p => p.Id).ToList();
        var leases = await mediator.Send(new GetAwaitingTenantLeasesQuery(propertyIds), ct);
        if (leases.Count == 0) return Ok(Array.Empty<object>());

        var userIds = leases.Select(l => l.TenantId)
            .Concat(leases.Where(l => l.BrokerId.HasValue).Select(l => l.BrokerId!.Value))
            .Distinct().ToList();
        var names = await mediator.Send(new GetUserNamesQuery(userIds), ct);
        var titlesById = properties.ToDictionary(p => p.Id, p => p.Title);

        var result = leases.Select(l => new
        {
            id                   = l.Id,
            propertyId           = l.PropertyId,
            propertyTitle        = titlesById.GetValueOrDefault(l.PropertyId, string.Empty),
            tenantId             = l.TenantId,
            tenantName           = names.GetValueOrDefault(l.TenantId, "Unknown"),
            brokerId             = l.BrokerId,
            brokerName           = l.BrokerId.HasValue ? names.GetValueOrDefault(l.BrokerId.Value) : null,
            installmentAmount    = l.InstallmentAmount,
            paymentFrequency     = l.PaymentFrequency,
            totalRentAmount      = l.TotalRentAmount,
            durationInMonths     = l.DurationInMonths,
            securityDeposit      = l.SecurityDeposit,
            startDate            = l.StartDate,
            endDate              = l.EndDate,
            paymentDayOfMonth    = l.PaymentDayOfMonth,
            managerApprovalStatus = l.ManagerApprovalStatus,
            approvedAt           = l.ManagerApprovedAt,
            managerNotes         = l.ManagerNotes,
            isFullManagement     = l.IsFullManagement,
            managementOptions    = ParseManagementOptions(l.ManagementOptions),
            uploadedAt           = l.CreatedAt,
            contractFileUrl      = l.ContractFileUrl
        });

        return Ok(result);
    }

    /// <summary>
    /// Active leases under the company's properties — tenant has accepted and lease is live.
    /// </summary>
    [HttpGet("mine/leases/active")]
    [Authorize(Policy = "CompanyManager")]
    public async Task<IActionResult> GetActiveLeases(CancellationToken ct)
    {
        var companyId = currentUser.CompanyId;
        if (companyId is null)
            return BadRequest(new { error = "No company is associated with your account." });

        var properties = await mediator.Send(new GetPropertiesByCompanyQuery(companyId.Value), ct);
        if (properties.Count == 0) return Ok(Array.Empty<object>());

        var propertyIds = properties.Select(p => p.Id).ToList();
        var leases = await mediator.Send(new GetActiveCompanyLeasesQuery(propertyIds), ct);
        if (leases.Count == 0) return Ok(Array.Empty<object>());

        var userIds = leases.Select(l => l.TenantId)
            .Concat(leases.Where(l => l.BrokerId.HasValue).Select(l => l.BrokerId!.Value))
            .Distinct().ToList();
        var names = await mediator.Send(new GetUserNamesQuery(userIds), ct);
        var titlesById = properties.ToDictionary(p => p.Id, p => p.Title);

        var result = leases.Select(l => new
        {
            id                = l.Id,
            propertyId        = l.PropertyId,
            propertyTitle     = titlesById.GetValueOrDefault(l.PropertyId, string.Empty),
            tenantId          = l.TenantId,
            tenantName        = names.GetValueOrDefault(l.TenantId, "Unknown"),
            brokerId          = l.BrokerId,
            brokerName        = l.BrokerId.HasValue ? names.GetValueOrDefault(l.BrokerId.Value) : null,
            installmentAmount = l.InstallmentAmount,
            paymentFrequency  = l.PaymentFrequency,
            totalRentAmount   = l.TotalRentAmount,
            durationInMonths  = l.DurationInMonths,
            securityDeposit   = l.SecurityDeposit,
            startDate         = l.StartDate,
            endDate           = l.EndDate,
            isFullManagement  = l.IsFullManagement,
            managementOptions = ParseManagementOptions(l.ManagementOptions),
            activeSince       = l.TenantConfirmedAt,
            uploadedAt        = l.CreatedAt
        });

        return Ok(result);
    }

    private static string[] ParseManagementOptions(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return [];
        try { return System.Text.Json.JsonSerializer.Deserialize<string[]>(raw) ?? []; }
        catch { return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries); }
    }

    // ── Company Wallet ────────────────────────────────────────────────────────

    [HttpGet("mine/wallet/balance")]
    [Authorize(Policy = "CompanyManager")]
    public async Task<IActionResult> GetWalletBalance(CancellationToken ct)
    {
        var companyId = currentUser.CompanyId;
        if (companyId is null) return BadRequest(new { error = "No company associated with your account." });

        var result = await mediator.Send(new Wallet.Application.Queries.GetCompanyWalletBalance.GetCompanyWalletBalanceQuery(companyId.Value), ct);
        return result is null
            ? Ok(new { companyId, balance = 0m, currency = "AED" })
            : Ok(result);
    }

    [HttpGet("mine/wallet/ledger")]
    [Authorize(Policy = "CompanyManager")]
    public async Task<IActionResult> GetWalletLedger(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var companyId = currentUser.CompanyId;
        if (companyId is null) return BadRequest(new { error = "No company associated with your account." });

        var result = await mediator.Send(new Wallet.Application.Queries.GetCompanyLedger.GetCompanyLedgerQuery(companyId.Value, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("mine/wallet/withdrawals")]
    [Authorize(Policy = "CompanyManager")]
    public async Task<IActionResult> GetWalletWithdrawals(CancellationToken ct)
    {
        var companyId = currentUser.CompanyId;
        if (companyId is null) return BadRequest(new { error = "No company associated with your account." });

        var result = await mediator.Send(new Wallet.Application.Queries.GetCompanyWithdrawals.GetCompanyWithdrawalsQuery(companyId.Value), ct);
        return Ok(result);
    }

    [HttpPost("mine/wallet/withdraw")]
    [Authorize(Policy = "CompanyManager")]
    public async Task<IActionResult> RequestWithdrawal([FromBody] CompanyWithdrawalRequest body, CancellationToken ct)
    {
        var companyId = currentUser.CompanyId;
        if (companyId is null) return BadRequest(new { error = "No company associated with your account." });

        try
        {
            var id = await mediator.Send(
                new Wallet.Application.Commands.RequestCompanyWithdrawal.RequestCompanyWithdrawalCommand(
                    companyId.Value, body.Amount, body.BankAccountDetails), ct);
            return Created($"/api/companies/mine/wallet/withdrawals/{id}", new { withdrawalId = id });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }
}

public record UpdateCompanyRequest(
    string Name,
    string City,
    string Address,
    string Phone,
    string? Website,
    string? Description,
    string? LogoUrl);

public record CompanyStatsResponse(
    int TotalProperties,
    int ActiveProperties,
    int PendingProperties,
    int RentedProperties,
    int TotalBrokers,
    int TotalViews,
    int TotalInquiries,
    int MonthlyInquiries,
    int DealsClosed);

public record CompanyActivityItem(string Type, string Text, DateTime Timestamp);

public record CompanyInquiryResponse(
    Guid Id,
    Guid PropertyId,
    string PropertyTitle,
    Guid TenantId,
    string TenantName,
    Guid? BrokerId,
    string? BrokerName,
    string Status,
    string Message,
    DateTime CreatedAt,
    DateTime? AcceptedAt,
    DateTime? ClosedAt);

public class UploadLogoRequest
{
    public IFormFile? File { get; init; }
}

public record CompanyWithdrawalRequest(decimal Amount, string BankAccountDetails);
