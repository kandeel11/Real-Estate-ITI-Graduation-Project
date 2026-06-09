using MediatR;
using AI.Application.Features.OwnershipVerification.DTOs;

namespace AI.Application.Features.OwnershipVerification.Commands.SubmitVerification
{
    public record SubmitOwnershipVerificationCommand(
        Guid PropertyId,
        Guid OwnerId,
        string OwnerName,
        byte[] NationalIdFile,
        string NationalIdMimeType,
        byte[] OwnershipProofFile,
        string OwnershipProofMimeType,
         string Country,
    string Governorate,
    string City,
    string? District,
    string Address
    ) : IRequest<VerificationStatusDto>;
}
