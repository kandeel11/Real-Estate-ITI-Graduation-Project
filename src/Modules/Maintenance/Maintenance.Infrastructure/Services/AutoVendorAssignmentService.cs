using Maintenance.Application.Interfaces;
using MaintenanceCompanies.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Maintenance.Infrastructure.Services;

public class AutoVendorAssignmentService(
    IMaintenanceModuleDbContext companiesDb,
    ICompanyAssignmentService companyAssignment)
    : IAutoVendorAssignmentService
{
    public async Task<Guid?> FindVendorForCategoryAsync(string aiCategoryText, CancellationToken ct = default)
    {
        var normalised = aiCategoryText.ToLowerInvariant();

        var categories = await companiesDb.MaintenanceCategories
            .Where(c => c.IsActive)
            .ToListAsync(ct);

        // Substring match in either direction: "Tenant Misuse / Plumbing" matches "Plumbing",
        // and "Plumbing" matches "Plumbing Repair".
        var match = categories.FirstOrDefault(c =>
        {
            var name = c.Name.ToLowerInvariant();
            return normalised.Contains(name) || name.Contains(normalised);
        });

        if (match is null)
            return null;

        return await companyAssignment.FindBestCompanyAsync(match.Id, city: string.Empty);
    }

    public async Task IncrementActiveTicketsAsync(Guid companyId, CancellationToken ct = default)
        => await companyAssignment.IncrementActiveTicketsAsync(companyId);
}
