using MediatR;
using AI.Application.Features.OwnershipVerification.DTOs;
using AI.Application.Features.OwnershipVerification.Interfaces;
using AI.Application.Interfaces.Common;

namespace AI.Application.Features.OwnershipVerification.Queries.GetVerificationStatus
{
    public class GetVerificationStatusHandler : IRequestHandler<GetVerificationStatusQuery, VerificationStatusDto>
    {
        private readonly IOwnershipVerifyRepository _repository;
        private readonly IBlobStorage _blobs;

        public GetVerificationStatusHandler(IOwnershipVerifyRepository repository, IBlobStorage blobs)
        {
            _repository = repository;
            _blobs = blobs;
        }

        public async Task<VerificationStatusDto> Handle(
            GetVerificationStatusQuery request,
            CancellationToken cancellationToken
        )
        {
            var verification = await _repository.GetByIdAsync(request.VerificationRequestId);

            if (verification == null)
                throw new KeyNotFoundException($"Verification request {request.VerificationRequestId} not found.");

            string? idUrl = null;
            string? proofUrl = null;

            if (!string.IsNullOrWhiteSpace(verification.NationalIdBlobPath))
                idUrl = await _blobs.GetSecureUrlAsync(verification.NationalIdBlobPath, TimeSpan.FromHours(1), cancellationToken);

            if (!string.IsNullOrWhiteSpace(verification.OwnershipProofBlobPath))
                proofUrl = await _blobs.GetSecureUrlAsync(verification.OwnershipProofBlobPath, TimeSpan.FromHours(1), cancellationToken);

            return new VerificationStatusDto
            {
                VerificationRequestId = verification.Id,
                Status = verification.Status.ToString(),
                AiConfidenceScore = verification.AiConfidenceScore,
                AiOcrExtractedOwnerName = verification.AiOcrExtractedName,
                AiOcrExtractedIdExpiry = verification.AiOcrExtractedId,
                AiFakeDetectionFlag = verification.AiFakeDetectionFlag,
                AdminReviewNotes = verification.AdminReviewNotes,
                AdminReviewedAt = verification.AdminReviewedAt,
                RejectionReason = verification.RejectionReason,
                NationalIdDocumentUrl = idUrl,
                OwnershipProofDocumentUrl = proofUrl
            };
        }
    }
}
