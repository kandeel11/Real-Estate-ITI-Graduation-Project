namespace Maintenance.Application.Interfaces;

public interface IAutoVendorAssignmentService
{
    Task<Guid?> FindVendorForCategoryAsync(string aiCategoryText, CancellationToken ct = default);
    Task IncrementActiveTicketsAsync(Guid companyId, CancellationToken ct = default);
}
