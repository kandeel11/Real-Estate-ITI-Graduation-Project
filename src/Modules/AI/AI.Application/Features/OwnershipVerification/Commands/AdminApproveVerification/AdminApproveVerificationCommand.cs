using MediatR;
using AI.Application.Features.OwnershipVerification.DTOs;

namespace AI.Application.Features.OwnershipVerification.Commands.AdminApproveVerification
{
    public record AdminApproveVerificationCommand(
        Guid VerificationRequestId,
        Guid AdminId,
        string ReviewNotes
    ) : IRequest<VerificationStatusDto>;
}
