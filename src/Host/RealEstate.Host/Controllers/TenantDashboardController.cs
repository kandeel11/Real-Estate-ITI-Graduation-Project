using Document.Application.Queries.Lease;
using Document.Application.Queries.Notification;
using Document.Application.Queries.Inquiry;
using Identity.Application.Interfaces;
using Maintenance.Application.Features.Queries.GetTicketStatistics;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstate.Host.Services;

namespace RealEstate.Host.Controllers;

[ApiController]
[Route("api/tenant/dashboard")]
[Authorize(Roles = "tenant")]
public class TenantDashboardController(
    ISender sender,
    ICurrentUserService currentUser,
    PropertyEnrichmentService enrichment) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var tenantId = currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        // Sequential — DbContext is not thread-safe, Task.WhenAll causes concurrency errors
        var stats = await sender.Send(new GetTicketStatisticsQuery(tenantId), ct);
        var lease = await sender.Send(new GetActiveLeaseQuery(tenantId), ct);
        var unread = await sender.Send(new GetUnreadNotificationCountQuery(tenantId), ct);
        var inquiries = await sender.Send(new GetMyInquiriesQuery(tenantId), ct);

        object? leaseCard = null;
        if (lease is not null)
        {
            var daysLeft = lease.EndDate.HasValue
                ? (int)(lease.EndDate.Value.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow.Date).TotalDays
                : (int?)null;

            leaseCard = new
            {
                leaseId = lease.Id,
                propertyId = lease.PropertyId,
                installmentAmount = lease.InstallmentAmount,
                startDate = lease.StartDate,
                endDate = lease.EndDate,
                daysUntilExpiry = daysLeft,
                isExpiringSoon = daysLeft.HasValue && daysLeft <= 30
            };
        }

        return Ok(new
        {
            tenant = new
            {
                id = currentUser.UserId,
                email = currentUser.Email,
                role = currentUser.Role
            },
            activeLease = leaseCard,
            ticketStats = stats,
            notifications = new { unreadCount = unread },
            inquiries = new
            {
                pending = inquiries.Count(i => i.Status == "Pending"),
                total = inquiries.Count
            }
        });
    }

    [HttpGet("activity")]
    public async Task<IActionResult> GetActivity(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var tenantId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var result = await sender.Send(
            new GetNotificationsQuery(tenantId, UnreadOnly: false, page, pageSize), ct);

        var activity = result.Items.Select(n => new
        {
            id = n.Id,
            type = n.Type,
            title = n.Title,
            description = n.Body,
            isRead = n.IsRead,
            relatedEntityType = n.RelatedEntityType,
            relatedEntityId = n.RelatedEntityId,
            createdAt = n.CreatedAt
        });

        return Ok(new { items = activity, total = result.Total, page, pageSize });
    }
}