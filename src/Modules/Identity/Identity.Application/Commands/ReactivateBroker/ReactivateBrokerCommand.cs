using MediatR;

namespace Identity.Application.Commands.ReactivateBroker;

/// <summary>
/// Re-enables a deactivated broker (and unblocks their login).
/// Mirror of DeactivateBrokerCommand.
/// </summary>
public record ReactivateBrokerCommand(
    Guid BrokerId,
    Guid? RequestingCompanyId,
    bool IsAdmin) : IRequest;
