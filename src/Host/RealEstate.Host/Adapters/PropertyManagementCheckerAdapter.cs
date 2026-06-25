using Identity.Application.Queries.GetCompanyDetails;
using Maintenance.Application.Interfaces;
using MediatR;
using Property.Application.Features.Queries.GetPropertyById;

namespace RealEstate.Host.Adapters;

/// <summary>
/// Bridges Property + Identity modules into Maintenance.Application's IPropertyManagementChecker.
/// Lives in Host so Maintenance stays free of cross-module references.
/// </summary>
public sealed class PropertyManagementCheckerAdapter(ISender sender) : IPropertyManagementChecker
{
    public async Task<PropertyManagementInfo?> GetAsync(Guid propertyId, CancellationToken ct = default)
    {
        var property = await sender.Send(new GetPropertyByIdQuery(propertyId), ct);
        if (property is null || property.CompanyId is null)
            return null;

        if (!property.IsFullManagement || !property.IsMaintenanceEnabled)
            return null;

        var company = await sender.Send(new GetCompanyDetailsQuery(property.CompanyId.Value), ct);
        if (company is null || !company.IsActive || !company.IsVerified)
            return null;

        return new PropertyManagementInfo(
            CompanyId: company.Id,
            CompanyManagerUserId: company.ManagerId,
            IsFullManagement: property.IsFullManagement,
            IsMaintenanceEnabled: property.IsMaintenanceEnabled);
    }
}
