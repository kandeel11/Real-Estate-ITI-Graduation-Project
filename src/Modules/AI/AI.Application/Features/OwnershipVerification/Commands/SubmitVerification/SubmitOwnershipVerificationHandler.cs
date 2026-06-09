using MediatR;
using AI.Application.Features.OwnershipVerification.DTOs;
using AI.Application.Features.OwnershipVerification.Interfaces;
using AI.Application.Common.Options;
using AI.Domain.Enums;
using Microsoft.Extensions.Options;

namespace AI.Application.Features.OwnershipVerification.Commands.SubmitVerification
{
    public class SubmitOwnershipVerificationHandler : IRequestHandler<SubmitOwnershipVerificationCommand, VerificationStatusDto>
    {
        private readonly IOwnershipVerifyRepository _repository;
        private readonly IOwnershipAiService _aiService;
        private readonly IPublisher _publisher;
        private readonly VerificationOptions _options;

        public SubmitOwnershipVerificationHandler(
            IOwnershipVerifyRepository repository,
            IOwnershipAiService aiService,
            IPublisher publisher,
            IOptions<VerificationOptions> options
        )
        {
            _repository = repository;
            _aiService = aiService;
            _publisher = publisher;
            _options = options.Value;
        }

        public async Task<VerificationStatusDto> Handle(
            SubmitOwnershipVerificationCommand request,
            CancellationToken cancellationToken
        )
        {
            // Guard: Check if owner already has an active verification request for this property
            var existing = await _repository.GetByPropertyIdAsync(request.PropertyId);
            if (existing != null && (existing.Status == VerificationStatus.Pending || existing.Status == VerificationStatus.RequiresManualReview))
            {
                throw new InvalidOperationException($"Property {request.PropertyId} already has an active verification request.");
            }

            // Create aggregate
            var verificationRequest = new AI.Domain.Entities.OwnershipVerificationRequest
            {
                PropertyId = request.PropertyId,
                OwnerId = request.OwnerId,
                Status = VerificationStatus.Pending
            };

            // Save initial state
            await _repository.AddAsync(verificationRequest);
            await _repository.SaveChangesAsync();

            try
            {
                // Call AI service for analysis
                var aiResult = await _aiService.AnalyzeAsync(
     nationalIdFile: request.NationalIdFile,
     nationalIdMimeType: request.NationalIdMimeType,
     ownershipProofFile: request.OwnershipProofFile,
     ownershipProofMimeType: request.OwnershipProofMimeType,
     ownerName: request.OwnerName,
     expectedLocation: new PropertyLocationDto(
                                 Country: request.Country,
                                 Governorate: request.Governorate,
                                 City: request.City,
                                 District: request.District,
                                 AddressLine: request.Address),
     ct: cancellationToken);

                // Update with AI results
                verificationRequest.AiConfidenceScore = aiResult.ConfidenceScore;
                verificationRequest.AiNameMatchScore = aiResult.NameMatchScore;
                verificationRequest.AiAuthenticityScore = aiResult.AuthenticityScore;
                verificationRequest.AiFakeDetectionFlag = aiResult.FakeDetectionFlag;
                verificationRequest.AiOcrExtractedName = aiResult.ExtractedOwnerName;
                verificationRequest.AiOcrExtractedId = aiResult.ExtractedIdExpiry;

                // Apply AI result logic
                if (aiResult.FakeDetectionFlag && _options.EnableFakeDocumentDetection)
                {
                    verificationRequest.Status = VerificationStatus.Rejected;
                    verificationRequest.RejectionReason = "Fake document detected by AI";

                    // Raise event
                    await _publisher.Publish(
                        new AI.Domain.Events.OwnershipRejectedEvent(
                            verificationRequest.Id,
                            request.PropertyId,
                            request.OwnerId,
                            "Fake document detected"
                        ),
                        cancellationToken
                    );
                }
                else if (aiResult.ConfidenceScore >= _options.AutoVerifyMinConfidence)
                {
                    verificationRequest.Status = VerificationStatus.AiVerified;

                    // Raise event
                    await _publisher.Publish(
                        new AI.Domain.Events.OwnershipAutoVerifiedEvent(
                            verificationRequest.Id,
                            request.PropertyId,
                            request.OwnerId,
                            aiResult.ConfidenceScore
                        ),
                        cancellationToken
                    );
                }
                else
                {
                    verificationRequest.Status = VerificationStatus.RequiresManualReview;

                    // Raise event
                    await _publisher.Publish(
                        new AI.Domain.Events.OwnershipSentToAdminReviewEvent(
                            verificationRequest.Id,
                            request.PropertyId,
                            request.OwnerId,
                            aiResult.ConfidenceScore,
                            aiResult.Recommendation
                        ),
                        cancellationToken
                    );
                }
            }
            catch (Exception ex)
            {
                verificationRequest.Status = VerificationStatus.RequiresManualReview;
                verificationRequest.AdminReviewNotes = $"AI analysis failed: {ex.Message}";
            }

            // Save final state
            await _repository.UpdateAsync(verificationRequest);
            await _repository.SaveChangesAsync();

            return new VerificationStatusDto
            {
                VerificationRequestId = verificationRequest.Id,
                Status = verificationRequest.Status.ToString(),
                AiConfidenceScore = verificationRequest.AiConfidenceScore,
                AiOcrExtractedOwnerName = verificationRequest.AiOcrExtractedName,
                AiOcrExtractedIdExpiry = verificationRequest.AiOcrExtractedId,
                AiFakeDetectionFlag = verificationRequest.AiFakeDetectionFlag,
                AdminReviewNotes = verificationRequest.AdminReviewNotes,
                AdminReviewedAt = verificationRequest.AdminReviewedAt,
                RejectionReason = verificationRequest.RejectionReason
            };
        }
    }
}
