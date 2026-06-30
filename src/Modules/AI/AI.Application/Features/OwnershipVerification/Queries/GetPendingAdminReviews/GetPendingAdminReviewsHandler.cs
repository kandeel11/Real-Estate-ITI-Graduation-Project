using MediatR;
using AI.Application.Features.OwnershipVerification.DTOs;
using AI.Application.Features.OwnershipVerification.Interfaces;
using AI.Application.Interfaces.Common;

namespace AI.Application.Features.OwnershipVerification.Queries.GetPendingAdminReviews
{
    public class GetPendingAdminReviewsHandler : IRequestHandler<GetPendingAdminReviewsQuery, List<VerificationStatusDto>>
    {
        private readonly IOwnershipVerifyRepository _repository;
        private readonly IBlobStorage _blobs;

        public GetPendingAdminReviewsHandler(IOwnershipVerifyRepository repository, IBlobStorage blobs)
        {
            _repository = repository;
            _blobs = blobs;
        }

        public async Task<List<VerificationStatusDto>> Handle(
            GetPendingAdminReviewsQuery request,
            CancellationToken cancellationToken
        )
        {
            var pending = await _repository.GetPendingAdminReviewsAsync(request.Limit);

            var tasks = pending.Select(async v =>
            {
                string? idUrl = null;
                string? proofUrl = null;

                if (!string.IsNullOrWhiteSpace(v.NationalIdBlobPath))
                    idUrl = await _blobs.GetSecureUrlAsync(v.NationalIdBlobPath, TimeSpan.FromHours(1), cancellationToken);

                if (!string.IsNullOrWhiteSpace(v.OwnershipProofBlobPath))
                    proofUrl = await _blobs.GetSecureUrlAsync(v.OwnershipProofBlobPath, TimeSpan.FromHours(1), cancellationToken);

                return new VerificationStatusDto
                {
                    VerificationRequestId = v.Id,
                    Status = v.Status.ToString(),
                    AiConfidenceScore = v.AiConfidenceScore,
                    AiOcrExtractedOwnerName = v.AiOcrExtractedName,
                    AiOcrExtractedIdExpiry = v.AiOcrExtractedId,
                    AiFakeDetectionFlag = v.AiFakeDetectionFlag,
                    AdminReviewNotes = v.AdminReviewNotes,
                    AdminReviewedAt = v.AdminReviewedAt,
                    RejectionReason = v.RejectionReason,
                    NationalIdDocumentUrl = idUrl,
                    OwnershipProofDocumentUrl = proofUrl
                };
            });

            return (await Task.WhenAll(tasks)).ToList();
        }
    }
}
