using MediatR;

namespace Document.Application.Commands.Lead.ManagerConfirmNoManagement;

/// <summary>
/// Called by a company manager to confirm a broker's "no management" close request.
/// Finalizes the deal: marks property as Rented and notifies the tenant.
/// </summary>
public record ManagerConfirmNoManagementCommand(
    Guid LeadId,
    Guid ManagerUserId
) : IRequest;
