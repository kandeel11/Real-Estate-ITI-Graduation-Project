using Document.Application.Queries.Inquiry;
using Document.Application.Queries.Lead.GetLeadsByBroker;
using Document.Application.Queries.Lead.GetLeadsByProperties;
using Document.Application.Queries.Lease;
using Document.Application.Queries.Lease.GetLeasePaymentSchedule;
using Document.Application.Queries.Lease.GetLeadsWithLeases;
using Document.Application.Queries.Lease.GetLeadLeaseStates;
using Document.Application.Queries.Message.GetInquiriesWithReply;
using Document.Application.Queries.Message.GetInquiryLastMessages;
using Document.Application.Queries.Notification;
using Identity.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payment.Application.Features.Queries.GetRentPaymentsByLeaseIds;
using Property.Application.DTOs;
using Property.Application.Features.Queries.GetPropertiesByBroker;
using Property.Application.Features.Queries.GetPropertyById;
using RealEstate.Host.Services;

namespace RealEstate.Host.Controllers;

/// <summary>
/// Self-service workspace for the logged-in broker. Mirrors TenantDashboardController.
/// Everything is scoped to the broker's identity UserId (the convention used across
/// the app for ListingBrokerId, lead assignment, and inquiry assignment).
/// </summary>
[ApiController]
[Route("api/brokers/me")]
[Authorize(Roles = "broker")]
public class BrokerDashboardController(
    ISender sender,
    ICurrentUserService currentUser,
    IBrokerRepository brokerRepo,
    IUserRepository userRepo,
    ICompanyRepository companyRepo,
    PropertyEnrichmentService enrichment) : ControllerBase
{
    private Guid UserId => currentUser.UserId ?? throw new UnauthorizedAccessException();

    // ── Profile ───────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var broker = await brokerRepo.GetByUserIdAsync(UserId, ct);
        if (broker is null) return NotFound(new { error = "Broker profile not found." });

        var user = await userRepo.GetByIdAsync(broker.UserId, ct);
        var (rank, total) = await GetRankAsync(broker.CompanyId, broker.UserId, ct);

        string companyName = string.Empty;
        if (broker.CompanyId is Guid cid)
        {
            var company = await companyRepo.GetByIdAsync(cid, ct);
            companyName = company?.Name ?? string.Empty;
        }

        return Ok(new
        {
            id = broker.UserId,
            name = user?.FullName ?? string.Empty,
            email = user?.Email ?? string.Empty,
            phone = user?.Phone ?? string.Empty,
            license = broker.LicenseNumber,
            companyName,
            bio = string.Empty,
            photo = user?.ProfileImageUrl,
            languages = Array.Empty<string>(),
            specializations = Array.Empty<string>(),
            rating = broker.RatingAvg,
            rankInCompany = rank,
            totalBrokers = total
        });
    }

    // ── Stats (overview cards) ──────────────────────────────────────────────────

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var broker = await brokerRepo.GetByUserIdAsync(UserId, ct);
        if (broker is null) return NotFound(new { error = "Broker profile not found." });

        var properties = await sender.Send(new GetPropertiesByBrokerQuery(UserId), ct);
        var propertyIds = properties.Select(p => p.Id).ToList();
        var inquiries = await sender.Send(new GetInquiriesByPropertiesQuery(propertyIds), ct);
        var (rank, total) = await GetRankAsync(broker.CompanyId, broker.UserId, ct);

        return Ok(new
        {
            assignedProperties = properties.Count,
            newInquiries = inquiries.Count(i => i.Status == "Pending"),
            closedDeals = broker.ClosedDealsCount,
            rating = broker.RatingAvg,
            rankInCompany = rank,
            totalBrokers = total
        });
    }

    // ── Performance ─────────────────────────────────────────────────────────────

    [HttpGet("performance")]
    public async Task<IActionResult> GetPerformance(CancellationToken ct)
    {
        var broker = await brokerRepo.GetByUserIdAsync(UserId, ct);
        if (broker is null) return NotFound(new { error = "Broker profile not found." });

        var properties = await sender.Send(new GetPropertiesByBrokerQuery(UserId), ct);
        var leads = await sender.Send(new GetLeadsByPropertiesQuery(properties.Select(p => p.Id).ToList()), ct);
        var (rank, total) = await GetRankAsync(broker.CompanyId, broker.UserId, ct);

        var now = DateTime.UtcNow;
        var won = leads.Where(l => l.Status == "ClosedWon").ToList();

        // Last 6 months of closed-won deals, oldest → newest
        var monthly = new List<object>(6);
        for (int i = 5; i >= 0; i--)
        {
            var month = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
            var deals = won.Count(l => l.UpdatedAt.Year == month.Year && l.UpdatedAt.Month == month.Month);
            monthly.Add(new { month = month.ToString("MMM"), deals });
        }

        var dealsThisMonth = won.Count(l => l.UpdatedAt.Year == now.Year && l.UpdatedAt.Month == now.Month);

        return Ok(new
        {
            dealsClosedThisMonth = dealsThisMonth,
            avgResponseTimeMinutes = broker.AvgResponseTimeMinutes,
            conversionRate = broker.ConversionRate,
            rating = broker.RatingAvg,
            ratingCount = broker.TotalRatings,
            rankInCompany = rank,
            totalBrokers = total.ToString(),
            monthlyDeals = monthly
        });
    }

    // ── Assigned properties ─────────────────────────────────────────────────────

    [HttpGet("properties")]
    public async Task<IActionResult> GetProperties(CancellationToken ct)
    {
        var properties = await sender.Send(new GetPropertiesByBrokerQuery(UserId), ct);
        await enrichment.EnrichAsync(properties, ct);
        return Ok(properties);
    }

    // ── Leads ───────────────────────────────────────────────────────────────────

    [HttpGet("leads")]
    public async Task<IActionResult> GetLeads(CancellationToken ct)
    {
        var properties = await sender.Send(new GetPropertiesByBrokerQuery(UserId), ct);
        var leads = await sender.Send(new GetLeadsByPropertiesQuery(properties.Select(p => p.Id).ToList()), ct);
        if (leads.Count == 0) return Ok(Array.Empty<object>());

        var tenantIds = leads.Select(l => l.TenantId).Distinct().ToList();
        var tenantNames = await userRepo.GetFullNamesByIdsAsync(tenantIds, ct);
        var propertyTitles = properties.ToDictionary(p => p.Id, p => p.Title);

        // Resolve tenant contact details so the broker never has to re-type them
        // (e.g. the lease-upload modal prefills the email from the tenant's account).
        var tenantContacts = new Dictionary<Guid, (string Email, string Phone)>();
        foreach (var tid in tenantIds)
        {
            var u = await userRepo.GetByIdAsync(tid, ct);
            if (u is not null) tenantContacts[tid] = (u.Email, u.Phone);
        }

        // Per-lead lease state → "Lease Created" (sent), "Resume" (un-sent draft), or "Create Lease".
        var leaseStates = await sender.Send(
            new GetLeadLeaseStatesQuery(leads.Select(l => l.Id).ToList()), ct);

        var result = leads.Select(l =>
        {
            var st = leaseStates.GetValueOrDefault(l.Id);
            return new
            {
                id = l.Id,
                tenantId = l.TenantId,
                tenantName = tenantNames.GetValueOrDefault(l.TenantId, string.Empty),
                tenantEmail = tenantContacts.GetValueOrDefault(l.TenantId).Email ?? string.Empty,
                tenantPhone = tenantContacts.GetValueOrDefault(l.TenantId).Phone ?? string.Empty,
                propertyId = l.PropertyId,
                propertyTitle = propertyTitles.GetValueOrDefault(l.PropertyId, string.Empty),
                status = MapLeadStatus(l.Status),
                stageEnteredAt = new DateTimeOffset(DateTime.SpecifyKind(l.UpdatedAt, DateTimeKind.Utc)).ToUnixTimeMilliseconds(),
                dealValue = l.DealValue?.ToString(),
                notes = l.Notes,
                hasLease = st?.HasSentLease ?? false,
                draftLeaseId = st?.DraftLeaseId
            };
        });

        return Ok(result);
    }

    // ── Leases I manage (Lease.BrokerId == me) ──────────────────────────────────

    [HttpGet("leases")]
    public async Task<IActionResult> GetLeases(CancellationToken ct)
    {
        // A broker is tied to leases through the properties they list (Lease has no
        // reliable BrokerId), mirroring how GetLeads resolves the broker's pipeline.
        var properties = await sender.Send(new GetPropertiesByBrokerQuery(UserId), ct);
        if (properties.Count == 0) return Ok(Array.Empty<object>());

        var leases = await sender.Send(
            new GetLeasesByPropertyIdsQuery(properties.Select(p => p.Id).ToList()), ct);
        if (leases.Count == 0) return Ok(Array.Empty<object>());

        var propertyTitles = properties.ToDictionary(p => p.Id, p => p.Title);

        var tenantNames = await userRepo.GetFullNamesByIdsAsync(
            leases.Select(l => l.TenantId).Distinct(), ct);

        // Predecessors that have an in-progress renewal draft (a non-terminal child lease).
        var renewalParents = LeasesWithRenewalDraft(leases);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = new List<object>(leases.Count);
        foreach (var l in leases)
        {
            var tenant = await userRepo.GetByIdAsync(l.TenantId, ct);
            result.Add(new
            {
                id = l.Id,
                propertyId = l.PropertyId,
                propertyTitle = propertyTitles.GetValueOrDefault(l.PropertyId, string.Empty),
                tenantName = tenantNames.GetValueOrDefault(l.TenantId, string.Empty),
                tenantEmail = tenant?.Email ?? string.Empty,
                startDate = l.StartDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                endDate = l.EndDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                installmentAmount = l.InstallmentAmount ?? 0m,
                totalRentAmount = l.TotalRentAmount,
                paymentFrequency = l.PaymentFrequency,
                durationInMonths = l.DurationInMonths,
                securityDeposit = l.SecurityDeposit ?? 0m,
                status = MapBrokerLeaseStatus(l, today),
                lifecycleStatus = l.Status,
                renewalRequestedAt = l.RenewalRequestedAt,
                renewalInProgress = renewalParents.Contains(l.Id),
                terminationRequestedAt = l.TerminationRequestedAt,
                terminationRequestedByTenant = l.TerminationRequestedById == l.TenantId,
                contractFileUrl = l.ContractFileUrl,
                extractionStatus = l.ExtractionStatus
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Ids of leases that have an in-progress renewal draft — a child lease
    /// (RenewedFromLeaseId == parent) that is not yet active and not rejected. Used so the
    /// UI can offer "Resume Renewal" and avoid spawning a duplicate draft.
    /// </summary>
    internal static HashSet<Guid> LeasesWithRenewalDraft(
        IEnumerable<Document.Application.Queries.Lease.GetLeaseById.LeaseDto> leases) =>
        leases
            .Where(c => c.RenewedFromLeaseId is not null
                        && !c.IsActive
                        && !string.Equals(c.ExtractionStatus, "rejected", StringComparison.OrdinalIgnoreCase))
            .Select(c => c.RenewedFromLeaseId!.Value)
            .ToHashSet();

    /// <summary>Legacy list-filter status (active/expiring/ended) the leases page chips use.</summary>
    private static string MapBrokerLeaseStatus(
        Document.Application.Queries.Lease.GetLeaseById.LeaseDto l, DateOnly today)
    {
        if (l.EndDate is DateOnly end)
        {
            if (end < today) return "ended";
            if (end <= today.AddDays(30)) return "expiring";
        }
        return "active";
    }

    // ── Inquiries assigned to me ────────────────────────────────────────────────

    [HttpGet("inquiries")]
    public async Task<IActionResult> GetInquiries(CancellationToken ct)
    {
        var properties = await sender.Send(new GetPropertiesByBrokerQuery(UserId), ct);
        var inquiries = await sender.Send(new GetInquiriesByPropertiesQuery(properties.Select(p => p.Id).ToList()), ct);
        if (inquiries.Count == 0) return Ok(Array.Empty<object>());

        var tenantNames = await userRepo.GetFullNamesByIdsAsync(
            inquiries.Select(i => i.TenantId).Distinct(), ct);
        var propertyTitles = properties.ToDictionary(p => p.Id, p => p.Title);
        var lastMessages = await sender.Send(
            new GetInquiryLastMessagesQuery(inquiries.Select(i => i.Id).ToList()), ct);
        var repliedSet = await sender.Send(
            new GetInquiriesWithReplyQuery(inquiries.Select(i => i.Id).ToList()), ct);

        // Newest activity first (chat list ordering).
        var ordered = inquiries
            .OrderByDescending(i => lastMessages.GetValueOrDefault(i.Id)?.At ?? i.CreatedAt)
            .ToList();

        var result = new List<object>(ordered.Count);
        foreach (var i in ordered)
        {
            var tenant = await userRepo.GetByIdAsync(i.TenantId, ct);
            var last = lastMessages.GetValueOrDefault(i.Id);
            result.Add(new
            {
                id = i.Id,
                propertyId = i.PropertyId,
                propertyTitle = propertyTitles.GetValueOrDefault(i.PropertyId, string.Empty),
                tenantId = i.TenantId,
                tenantName = tenantNames.GetValueOrDefault(i.TenantId, string.Empty),
                tenantEmail = tenant?.Email ?? string.Empty,
                message = i.Message,
                status = MapInquiryStatus(i.Status, repliedSet.Contains(i.Id)),
                createdAt = i.CreatedAt,
                // Latest activity for the chat list (falls back to the inquiry itself).
                lastMessage = last?.Text ?? i.Message,
                lastMessageAt = last?.At ?? i.CreatedAt
            });
        }

        return Ok(result);
    }

    // ── Payments ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// All rent payments for leases managed by this broker (where Lease.BrokerId == UserId).
    /// Returns RentPaymentDto[] enriched with propertyId/tenantId/landlordId.
    /// </summary>
    [HttpGet("payments")]
    public async Task<IActionResult> GetPayments(CancellationToken ct)
    {
        var leases = await sender.Send(new GetLeasesByBrokerQuery(UserId), ct);
        if (leases.Count == 0) return Ok(Array.Empty<object>());

        var leaseIds = leases.Select(l => l.Id).ToList();
        var payments = await sender.Send(new GetRentPaymentsByLeaseIdsQuery(leaseIds), ct);
        return Ok(payments);
    }

    /// <summary>
    /// Payment schedule for every active lease managed by this broker.
    /// Returns one schedule entry per lease, enriched with property title.
    /// </summary>
    [HttpGet("payments/schedule")]
    public async Task<IActionResult> GetPaymentsSchedule(CancellationToken ct)
    {
        var leases = await sender.Send(new GetLeasesByBrokerQuery(UserId), ct);
        var activeLeases = leases.Where(l => l.IsActive).ToList();
        if (activeLeases.Count == 0) return Ok(Array.Empty<object>());

        var propertyTitles = new Dictionary<Guid, string>();
        foreach (var pid in activeLeases.Select(l => l.PropertyId).Distinct())
        {
            var prop = await sender.Send(new GetPropertyByIdQuery(pid), ct);
            if (prop is not null) propertyTitles[pid] = prop.Title;
        }

        var result = new List<object>(activeLeases.Count);
        foreach (var lease in activeLeases)
        {
            var schedule = await sender.Send(new GetLeasePaymentScheduleQuery(lease.Id), ct);
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

    // ── Ratings ─────────────────────────────────────────────────────────────────
    // Ratings target leases/properties, not brokers, so there is no per-broker
    // review feed yet. The aggregate (RatingAvg/TotalRatings) is in the profile.
    [HttpGet("ratings")]
    public IActionResult GetRatings() => Ok(Array.Empty<object>());

    // ── Activity (from notifications) ────────────────────────────────────────────

    [HttpGet("activity")]
    public async Task<IActionResult> GetActivity(CancellationToken ct)
    {
        var result = await sender.Send(
            new GetNotificationsQuery(UserId, UnreadOnly: false, Page: 1, PageSize: 10), ct);

        var activity = result.Items.Select(n => new
        {
            id = n.Id,
            text = n.Title,
            timeAgo = TimeAgo(n.CreatedAt)
        });

        return Ok(activity);
    }

    // ── Tasks (derived from real workload) ───────────────────────────────────────

    [HttpGet("tasks")]
    public async Task<IActionResult> GetTasks(CancellationToken ct)
    {
        var properties = await sender.Send(new GetPropertiesByBrokerQuery(UserId), ct);
        var propertyIds = properties.Select(p => p.Id).ToList();
        var inquiries = await sender.Send(new GetInquiriesByPropertiesQuery(propertyIds), ct);
        var leads = await sender.Send(new GetLeadsByPropertiesQuery(propertyIds), ct);

        var tasks = new List<object>();
        var newInquiries = inquiries.Count(i => i.Status == "Pending");
        if (newInquiries > 0)
            tasks.Add(new { id = "new-inquiries", text = $"Respond to {newInquiries} new inquir{(newInquiries == 1 ? "y" : "ies")}", done = false });

        var negotiating = leads.Count(l => l.Status == "Negotiating");
        if (negotiating > 0)
            tasks.Add(new { id = "negotiating", text = $"Follow up on {negotiating} lead{(negotiating == 1 ? "" : "s")} in negotiation", done = false });

        return Ok(tasks);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    /// <summary>Rank within the broker's company by closed deals; (1,1) for independents.</summary>
    private async Task<(int rank, int total)> GetRankAsync(Guid? companyId, Guid brokerUserId, CancellationToken ct)
    {
        if (companyId is not Guid cid) return (1, 1);

        var peers = await brokerRepo.GetByCompanyIdAsync(cid, ct);
        if (peers.Count == 0) return (1, 1);

        var ordered = peers
            .OrderByDescending(b => b.ClosedDealsCount)
            .ThenByDescending(b => b.RatingAvg)
            .ToList();
        var idx = ordered.FindIndex(b => b.UserId == brokerUserId);
        return (idx < 0 ? ordered.Count : idx + 1, ordered.Count);
    }

    private static string MapLeadStatus(string status) => status switch
    {
        "New" => "new",
        "Contacted" => "contacted",
        "Negotiating" => "negotiating",
        "ClosedWon" => "closed_won",
        "ClosedLost" => "closed_lost",
        _ => "new"
    };

    private static string MapInquiryStatus(string status, bool hasReply) => status switch
    {
        "Closed" => "converted",
        "Accepted" => "replied",
        // A pending inquiry counts as "replied" once the broker/owner has sent a message.
        _ => hasReply ? "replied" : "new"
    };

    private static string TimeAgo(DateTime utc)
    {
        var span = DateTime.UtcNow - DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        if (span.TotalMinutes < 1) return "just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
        return $"{(int)span.TotalDays}d ago";
    }
}
