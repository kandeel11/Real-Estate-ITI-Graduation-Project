using MediatR;
using AI.Application.Features.OwnershipVerification.DTOs;

namespace AI.Application.Features.OwnershipVerification.Commands.AdminRejectVerification
{
    public record AdminRejectVerificationCommand(
        Guid VerificationRequestId,
        Guid AdminId,
        string RejectionReason
    ) : IRequest<VerificationStatusDto>;
}
