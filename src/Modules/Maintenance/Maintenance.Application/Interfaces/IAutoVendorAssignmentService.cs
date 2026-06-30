using Maintenance.Application.DTOs.Responses;

namespace Maintenance.Application.Interfaces;

public interface IAutoVendorAssignmentService
{
    /// <summary>
    /// Returns all active category names exactly as stored in the DB (e.g. "Plumbing", "Electrical").
    /// Pass this list to the AI so it returns an exact name instead of a free-form description.
    /// </summary>
    Task<IReadOnlyList<string>> GetCategoryNamesAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns all active maintenance companies that match the AI category and city.
    /// Falls back to all cities when none found in the given city.
    /// </summary>
    Task<IReadOnlyList<AvailableVendorDto>> GetAvailableVendorsAsync(
        string aiCategoryText, string city, CancellationToken ct = default);

    Task<Guid?> FindVendorForCategoryAsync(string aiCategoryText, string city, CancellationToken ct = default);
    Task IncrementActiveTicketsAsync(Guid companyId, CancellationToken ct = default);
}
