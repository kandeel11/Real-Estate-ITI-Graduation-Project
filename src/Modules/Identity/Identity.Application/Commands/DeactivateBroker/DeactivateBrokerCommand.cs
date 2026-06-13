using MediatR;

namespace Identity.Application.Commands.DeactivateBroker;

/// <summary>
/// Deactivates a broker (and their login). Allowed for the broker's own company manager,
/// or an admin. Soft action — the broker/user rows remain but are marked inactive.
/// </summary>
public record DeactivateBrokerCommand(
    Guid BrokerId,
    Guid? RequestingCompanyId,
    bool IsAdmin
) : IRequest;
