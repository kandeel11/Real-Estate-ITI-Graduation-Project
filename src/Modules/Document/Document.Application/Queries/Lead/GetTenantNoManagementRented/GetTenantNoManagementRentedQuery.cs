using MediatR;

namespace Document.Application.Queries.Lead.GetTenantNoManagementRented;

/// <summary>
/// Returns PropertyIds for leads that are ClosedWon with no associated lease —
/// i.e., deals closed via the no-management path. Used to populate the tenant's
/// "My Property" page when there is no lease document to link to.
/// </summary>
public record GetTenantNoManagementRentedQuery(Guid TenantId) : IRequest<List<Guid>>;
