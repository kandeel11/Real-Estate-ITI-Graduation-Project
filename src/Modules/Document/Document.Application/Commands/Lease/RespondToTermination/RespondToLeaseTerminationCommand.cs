using MediatR;

namespace Document.Application.Commands.Lease.RespondToTermination;

/// <summary>
/// The counterparty's answer to a pending early-termination request.
/// Approve ends the lease (and returns the property to market); Reject clears the
/// request and the lease continues unchanged.
/// </summary>
public record RespondToLeaseTerminationCommand(
    Guid LeaseId,
    Guid ResponderId,
    bool Approve,
    string? ResponseNote) : IRequest;
