using MediatR;

namespace Document.Application.Commands.Lease.RequestTermination;

/// <summary>
/// One party (tenant OR owner/broker) proposes ending an active lease early.
/// Does not end the lease — it records the request and notifies the other party,
/// who must approve via <c>RespondToLeaseTerminationCommand</c>.
/// </summary>
public record RequestLeaseTerminationCommand(
    Guid LeaseId,
    Guid RequesterId,
    string Reason,
    DateOnly? EffectiveDate) : IRequest;
