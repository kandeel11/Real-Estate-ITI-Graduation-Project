using MediatR;

namespace Document.Application.Commands.Lease.RequestRenewal;

/// <summary>
/// The tenant asks to renew an active lease near its end. Records the request and
/// notifies the landlord (owner + broker), who fulfils it by uploading a new lease
/// contract (which runs the same AI-extraction → send-to-tenant cycle).
/// </summary>
public record RequestLeaseRenewalCommand(Guid LeaseId, Guid TenantId) : IRequest;
