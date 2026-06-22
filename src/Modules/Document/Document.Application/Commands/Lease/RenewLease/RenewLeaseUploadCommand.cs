using Document.Application.Commands.Lease.UploadLeaseContract;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Document.Application.Commands.Lease.RenewLease;

/// <summary>
/// Landlord (owner/broker) uploads a fresh contract to renew an existing lease.
/// Creates a NEW lease linked to the predecessor via RenewedFromLeaseId, copying the
/// property/tenant/owner/broker, and kicks off the same AI-extraction pipeline as a
/// first-time upload. The predecessor is superseded only when the new lease activates.
/// </summary>
public record RenewLeaseUploadCommand(
    Guid PreviousLeaseId,
    Guid RequesterId,
    IFormFile File) : IRequest<UploadLeaseResponse>;
