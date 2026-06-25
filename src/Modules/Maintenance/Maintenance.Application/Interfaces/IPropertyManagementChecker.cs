namespace Maintenance.Application.Interfaces;

/// <summary>
/// Checks whether a property is under full company management.
/// Implemented by a Host-layer adapter that bridges into Property and Identity modules.
/// This interface keeps Maintenance.Application free of cross-module dependencies.
/// </summary>
public interface IPropertyManagementChecker
{
    /// <summary>
    /// Returns management info when the property has an active management company
    /// with IsFullManagement=true and IsMaintenanceEnabled=true; otherwise null.
    /// </summary>
    Task<PropertyManagementInfo?> GetAsync(Guid propertyId, CancellationToken ct = default);
}

public sealed record PropertyManagementInfo(
    Guid CompanyId,
    Guid CompanyManagerUserId,
    bool IsFullManagement,
    bool IsMaintenanceEnabled);
