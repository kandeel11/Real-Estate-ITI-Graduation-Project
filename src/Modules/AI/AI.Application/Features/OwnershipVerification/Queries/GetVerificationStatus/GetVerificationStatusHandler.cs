using MediatR;
using AI.Application.Features.OwnershipVerification.DTOs;
using AI.Application.Features.OwnershipVerification.Interfaces;

namespace AI.Application.Features.OwnershipVerification.Queries.GetVerificationStatus
{
    public class GetVerificationStatusHandler : IRequestHandler<GetVerificationStatusQuery, VerificationStatusDto>
    {
        private readonly IOwnershipVerifyRepository _repository;

        public GetVerificationStatusHandler(IOwnershipVerifyRepository repository)
        {
            _repository = repository;
        }

        public async Task<VerificationStatusDto> Handle(
            GetVerificationStatusQuery request,
            CancellationToken cancellationToken
        )
        {
            var verification = await _repository.GetByIdAsync(request.VerificationRequestId);

            if (verification == null)
                throw new KeyNotFoundException($"Verification request {request.VerificationRequestId} not found.");

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
                RejectionReason = verification.RejectionReason
            };
        }
    }
}
