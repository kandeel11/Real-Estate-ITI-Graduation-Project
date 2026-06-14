using MediatR;
using AI.Application.Features.OwnershipVerification.DTOs;
using AI.Application.Features.OwnershipVerification.Interfaces;

namespace AI.Application.Features.OwnershipVerification.Queries.GetPendingAdminReviews
{
    public class GetPendingAdminReviewsHandler : IRequestHandler<GetPendingAdminReviewsQuery, List<VerificationStatusDto>>
    {
        private readonly IOwnershipVerifyRepository _repository;

        public GetPendingAdminReviewsHandler(IOwnershipVerifyRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<VerificationStatusDto>> Handle(
            GetPendingAdminReviewsQuery request,
            CancellationToken cancellationToken
        )
        {
            var pending = await _repository.GetPendingAdminReviewsAsync(request.Limit);

            return pending.Select(v => new VerificationStatusDto
            {
                VerificationRequestId = v.Id,
                Status = v.Status.ToString(),
                AiConfidenceScore = v.AiConfidenceScore,
                AiOcrExtractedOwnerName = v.AiOcrExtractedName,
                AiOcrExtractedIdExpiry = v.AiOcrExtractedId,
                AiFakeDetectionFlag = v.AiFakeDetectionFlag,
                AdminReviewNotes = v.AdminReviewNotes,
                AdminReviewedAt = v.AdminReviewedAt,
                RejectionReason = v.RejectionReason
            }).ToList();
        }
    }
}
