using MediatR;

namespace Document.Application.Commands.Lease.TerminateLease;

public record TerminateLeaseCommand(Guid LeaseId, string Reason) : IRequest;