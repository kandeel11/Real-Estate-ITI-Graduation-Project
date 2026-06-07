using MediatR;
using AI.Application.Features.OwnershipVerification.DTOs;
using AI.Application.Features.OwnershipVerification.Interfaces;
using AI.Domain.Enums;

namespace AI.Application.Features.OwnershipVerification.Commands.AdminRejectVerification
{
    public class AdminRejectVerificationHandler : IRequestHandler<AdminRejectVerificationCommand, VerificationStatusDto>
    {
        private readonly IOwnershipVerifyRepository _repository;
        private readonly IPublisher _publisher;

        public AdminRejectVerificationHandler(
            IOwnershipVerifyRepository repository,
            IPublisher publisher
        )
        {
            _repository = repository;
            _publisher = publisher;
        }

        public async Task<VerificationStatusDto> Handle(
            AdminRejectVerificationCommand request,
            CancellationToken cancellationToken
        )
        {
            var verification = await _repository.GetByIdAsync(request.VerificationRequestId);

            if (verification == null)
                throw new KeyNotFoundException($"Verification request {request.VerificationRequestId} not found.");

            // Update status
            verification.Status = VerificationStatus.Rejected;
            verification.AdminReviewerId = request.AdminId;
            verification.RejectionReason = request.RejectionReason;
            verification.AdminReviewedAt = DateTime.UtcNow;

            // Raise event
            await _publisher.Publish(
                new AI.Domain.Events.OwnershipRejectedEvent(
                    verification.Id,
                    verification.PropertyId,
                    verification.OwnerId,
                    request.RejectionReason
                ),
                cancellationToken
            );

            // Persist
            await _repository.UpdateAsync(verification);
            await _repository.SaveChangesAsync();

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
