using Maintenance.Application.DTOs.Responses;
using Maintenance.Application.Interfaces;
using MaintenanceCompanies.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Maintenance.Infrastructure.Services;

public class AutoVendorAssignmentService(
    IMaintenanceModuleDbContext companiesDb,
    ICompanyAssignmentService companyAssignment)
    : IAutoVendorAssignmentService
{
    public async Task<IReadOnlyList<string>> GetCategoryNamesAsync(CancellationToken ct = default)
        => await companiesDb.MaintenanceCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => c.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AvailableVendorDto>> GetAvailableVendorsAsync(
        string aiCategoryText, string city, CancellationToken ct = default)
    {
        var categoryId = await MatchCategoryAsync(aiCategoryText, ct);
        if (categoryId is null) return [];

        var cityLower = city.Trim().ToLowerInvariant();

        var baseQuery = companiesDb.MaintenanceCompanies
            .Where(c => c.IsActive)
            .Where(c => c.Categories.Any(cc => cc.CategoryId == categoryId.Value));

        // Prefer same-city companies; fall back to all cities
        var inCity = await baseQuery
            .Where(c => c.City.ToLower() == cityLower)
            .OrderByDescending(c => c.AverageRating)
            .ThenBy(c => c.ActiveTicketsCount)
            .ToListAsync(ct);

        var companies = inCity.Count > 0
            ? inCity
            : await baseQuery
                .OrderByDescending(c => c.AverageRating)
                .ThenBy(c => c.ActiveTicketsCount)
                .ToListAsync(ct);

        return companies
            .Select(c => new AvailableVendorDto(
                c.Id, c.CompanyName, c.City, c.AverageRating,
                c.ActiveTicketsCount, c.CompletedTicketsCount,
                c.Description, c.LogoUrl))
            .ToList();
    }

    public async Task<Guid?> FindVendorForCategoryAsync(
        string aiCategoryText, string city, CancellationToken ct = default)
    {
        var match = await MatchCategoryAsync(aiCategoryText, ct);
        if (match is null) return null;
        return await companyAssignment.FindBestCompanyAsync(match.Value, city);
    }

    // Returns the CategoryId whose name best matches the AI category text, or null.
    private async Task<Guid?> MatchCategoryAsync(string aiCategoryText, CancellationToken ct)
    {
        var normalised = aiCategoryText.ToLowerInvariant();
        var categories = await companiesDb.MaintenanceCategories
            .Where(c => c.IsActive)
            .ToListAsync(ct);

        var match = categories.FirstOrDefault(c =>
        {
            var name = c.Name.ToLowerInvariant();
            return normalised.Contains(name) || name.Contains(normalised);
        });

        return match?.Id;
    }

    public async Task IncrementActiveTicketsAsync(Guid companyId, CancellationToken ct = default)
        => await companyAssignment.IncrementActiveTicketsAsync(companyId);
}
