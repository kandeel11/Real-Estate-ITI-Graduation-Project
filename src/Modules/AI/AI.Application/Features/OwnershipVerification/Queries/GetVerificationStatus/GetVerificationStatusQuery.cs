using MediatR;
using AI.Application.Features.OwnershipVerification.DTOs;

namespace AI.Application.Features.OwnershipVerification.Queries.GetVerificationStatus
{
    public record GetVerificationStatusQuery(
        Guid VerificationRequestId
    ) : IRequest<VerificationStatusDto>;
}
