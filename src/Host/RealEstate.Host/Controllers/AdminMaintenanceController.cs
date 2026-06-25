using Document.Application.Queries.Lead.GetClosedWonLeads;
using Identity.Application.Commands.RecomputeBrokerDeals;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Property.Application.Features.Queries.GetAllProperties;
using Property.Application.Features.Queries.GetFullyManagedProperties;

namespace RealEstate.Host.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = "AdminOnly")]
public class AdminMaintenanceController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// One-time (idempotent, re-runnable) backfill: rebuilds every broker's
    /// ClosedDealsCount from the source of truth — closed-won leads on the
    /// listings they manage. Credits the lead's assigned broker, falling back to
    /// the property's listing broker when no assignment row exists.
    /// </summary>
    [HttpPost("recompute-broker-deals")]
    public async Task<IActionResult> RecomputeBrokerDeals(CancellationToken ct)
    {
        var wonLeads = await mediator.Send(new GetClosedWonLeadsQuery(), ct);
        var properties = await mediator.Send(new GetAllPropertiesQuery(), ct);

        var listingBrokerByProperty = properties
            .Where(p => p.ListingBrokerId.HasValue)
            .ToDictionary(p => p.Id, p => p.ListingBrokerId!.Value);

        var counts = new Dictionary<Guid, int>();
        var skipped = 0;
        foreach (var lead in wonLeads)
        {
            var brokerUserId = lead.AssignedBrokerId
                ?? (listingBrokerByProperty.TryGetValue(lead.PropertyId, out var b) ? b : (Guid?)null);

            if (brokerUserId is null) { skipped++; continue; }

            counts[brokerUserId.Value] = counts.GetValueOrDefault(brokerUserId.Value) + 1;
        }

        var brokersUpdated = await mediator.Send(new RecomputeBrokerDealsCommand(counts), ct);

        return Ok(new
        {
            wonLeads = wonLeads.Count,
            leadsWithoutBroker = skipped,
            brokersUpdated
        });
    }

    /// <summary>
    /// Returns all rented properties under full company management.
    /// Each entry includes which services are enabled (maintenance, payment).
    /// </summary>
    [HttpGet("properties/fully-managed")]
    public async Task<IActionResult> GetFullyManagedProperties(CancellationToken ct)
    {
        var properties = await mediator.Send(new GetFullyManagedPropertiesQuery(), ct);
        return Ok(new
        {
            total = properties.Count,
            properties
        });
    }
}
