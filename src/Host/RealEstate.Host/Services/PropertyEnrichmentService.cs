using Identity.Application.Interfaces;
using Property.Application.DTOs;

namespace RealEstate.Host.Services;

/// <summary>
/// Resolves company names and broker names for property DTOs.
/// Lives in Host because it crosses the Property and Identity module boundary.
/// </summary>
public class PropertyEnrichmentService
{
    private readonly ICompanyRepository _companyRepo;
    private readonly IUserRepository _userRepo;

    public PropertyEnrichmentService(ICompanyRepository companyRepo, IUserRepository userRepo)
    {
        _companyRepo = companyRepo;
        _userRepo = userRepo;
    }

    public async Task EnrichAsync(IEnumerable<PropertySummaryDto> properties, CancellationToken ct = default)
    {
        var list = properties.ToList();
        if (list.Count == 0) return;

        // Batch-fetch company names
        var companyIds = list.Where(p => p.CompanyId.HasValue).Select(p => p.CompanyId!.Value).Distinct();
        var companyNames = await _companyRepo.GetNamesByIdsAsync(companyIds, ct);

        // Batch-fetch broker full names (only for properties that have a broker assigned)
        var brokerUserIds = list.Where(p => p.ListingBrokerId.HasValue).Select(p => p.ListingBrokerId!.Value).Distinct();
        var brokerNames = await _userRepo.GetFullNamesByIdsAsync(brokerUserIds, ct);

        // For listings with no company and no broker, fetch owner names
        var ownerIds = list.Where(p => !p.CompanyId.HasValue && !p.ListingBrokerId.HasValue).Select(p => p.OwnerId).Distinct();
        var ownerNames = await _userRepo.GetFullNamesByIdsAsync(ownerIds, ct);

        foreach (var property in list)
        {
            if (property.CompanyId.HasValue && companyNames.TryGetValue(property.CompanyId.Value, out var companyName))
            {
                property.ListingDisplayName = companyName;
            }
            else if (property.ListingBrokerId.HasValue && brokerNames.TryGetValue(property.ListingBrokerId.Value, out var brokerDisplayName))
            {
                property.ListingDisplayName = brokerDisplayName;
            }
            else if (ownerNames.TryGetValue(property.OwnerId, out var ownerName))
            {
                property.ListingDisplayName = ownerName;
            }

            if (property.ListingBrokerId.HasValue && brokerNames.TryGetValue(property.ListingBrokerId.Value, out var brokerName))
            {
                property.ListingBrokerName = brokerName;
            }
        }
    }

    public async Task EnrichAsync(PropertyDetailDto property, CancellationToken ct = default)
    {
        if (property.CompanyId.HasValue)
        {
            var companyNames = await _companyRepo.GetNamesByIdsAsync([property.CompanyId.Value], ct);
            if (companyNames.TryGetValue(property.CompanyId.Value, out var companyName))
                property.ListingDisplayName = companyName;
        }

        if (property.ListingBrokerId.HasValue)
        {
            var brokerNames = await _userRepo.GetFullNamesByIdsAsync([property.ListingBrokerId.Value], ct);
            if (brokerNames.TryGetValue(property.ListingBrokerId.Value, out var brokerName))
            {
                property.ListingBrokerName = brokerName;
                if (!property.CompanyId.HasValue)
                    property.ListingDisplayName = brokerName;
            }
        }
        else if (!property.CompanyId.HasValue)
        {
            var ownerNames = await _userRepo.GetFullNamesByIdsAsync([property.OwnerId], ct);
            if (ownerNames.TryGetValue(property.OwnerId, out var ownerName))
                property.ListingDisplayName = ownerName;
        }
    }
}
