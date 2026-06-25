using Identity.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Property.Application.Interfaces;

namespace RealEstate.Host.Controllers;

[ApiController]
[Route("api/properties")]
public class PropertyOwnershipController(
    IPropertyRepository propertyRepo,
    IUserRepository userRepo,
    IBrokerRepository brokerRepo,
    ICompanyRepository companyRepo) : ControllerBase
{
    /// <summary>
    /// Resolves ownership and broker type for a property.
    /// Returns one of three shapes:
    ///   - DirectOwner: no listing broker, returns owner name and verification status
    ///   - IndependentBroker: broker has no company affiliation
    ///   - CompanyBroker: broker belongs to a company — includes company name
    /// </summary>
    [HttpGet("{id:guid}/ownership")]
    public async Task<IActionResult> GetOwnership(Guid id, CancellationToken ct)
    {
        var property = await propertyRepo.GetByIdAsync(id, ct);
        if (property is null)
            return NotFound(new { error = $"Property {id} not found." });

        // No broker assigned → Direct Owner
        if (property.ListingBrokerId is null)
        {
            var owner = await userRepo.GetByIdAsync(property.OwnerId, ct);
            return Ok(new
            {
                ownershipType = "DirectOwner",
                responsiblePartyId = property.OwnerId,
                ownerName = owner?.FullName ?? "Unknown",
                isOwnerVerified = property.OwnerShipStatus?.Equals("verified", StringComparison.OrdinalIgnoreCase),
                brokerName = (string?)null,
                brokerId = (Guid?)null,
                companyName = (string?)null,
                companyId = (Guid?)null
            });
        }

        // Broker assigned → resolve broker details
        var broker = await brokerRepo.GetByUserIdAsync(property.ListingBrokerId.Value, ct);
        var brokerUser = broker is not null ? await userRepo.GetByIdAsync(broker.UserId, ct) : null;
        var brokerName = brokerUser?.FullName ?? "Unknown Broker";

        if (broker?.CompanyId is null)
        {
            // Independent broker
            return Ok(new
            {
                ownershipType = "IndependentBroker",
                responsiblePartyId = property.ListingBrokerId.Value,
                ownerName = (string?)null,
                isOwnerVerified = (bool?)null,
                brokerName,
                brokerId = property.ListingBrokerId,
                companyName = (string?)null,
                companyId = (Guid?)null
            });
        }

        // Company broker
        var company = await companyRepo.GetByIdAsync(broker.CompanyId.Value, ct);
        return Ok(new
        {
            ownershipType = "CompanyBroker",
            responsiblePartyId = broker.CompanyId.Value,
            ownerName = (string?)null,
            isOwnerVerified = (bool?)null,
            brokerName,
            brokerId = property.ListingBrokerId,
            companyName = company?.Name ?? "Unknown Company",
            companyId = broker.CompanyId
        });
    }
}
