using AI.Domain.Enums;
using Domain.Entities;

namespace AI.Domain.Entities
{

    public class OwnershipVerificationRequest : BaseEntity
    {
        public Guid PropertyId { get; set; } // ref property.properties 
        public Guid OwnerId { get; set; } // ref identity.users 

        public VerificationStatus Status { get; set; } = VerificationStatus.Pending;
        public string? FailureReason { get; private set; }
        public decimal? AiConfidenceScore { get; set; }
        public decimal? AiNameMatchScore { get; set; }
        public decimal? AiAuthenticityScore { get; set; }
        public bool AiFakeDetectionFlag { get; set; } = false;
        public string? AiOcrExtractedName { get; set; }
        public string? AiOcrExtractedId { get; set; }
        public string? ExtractedCountry { get; private set; }
        public string? ExtractedGovernorate { get; private set; }
        public string? ExtractedCity { get; private set; }
        public string? ExtractedDistrict { get; private set; }
        public string? ExtractedAddress { get; private set; }

        public decimal? LocationMatchScore { get; private set; }
        public bool? LocationMismatchFlag { get; private set; }
        public string? LocationMatchNotes { get; private set; }
        // NEW: From Azure Document Intelligence extraction
        public string? AiOcrExtractedIdExpiry { get; set; }
        public string? AiOcrExtractedOwnerName { get; set; }

        public Guid? AdminReviewerId { get; set; }
        public string? AdminReviewNotes { get; set; }
        public DateTime? AdminReviewedAt { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime? CompletedAt { get; private set; }
        public string OwnerFullName { get; private set; }
        public string NationalIdBlobPath { get; private set; }
        public string OwnershipProofBlobPath { get; private set; }


        // AI.Domain/Entities/OwnershipVerificationRequest.cs

        public void ApplyAiResult(
            decimal confidenceScore,
            decimal nameMatchScore,
            decimal authenticityScore,
            bool fakeDetectionFlag,
            string recommendation,
            string? analysisNotes,
            string? ocrExtractedName,
            string? ocrExtractedId,
            string? extractedCountry,
            string? extractedGovernorate,
            string? extractedCity,
            string? extractedDistrict,
            string? extractedAddress,
            decimal locationMatchScore,
            bool locationMismatchFlag,
            string? locationMatchNotes)
        {
            AiConfidenceScore = confidenceScore;
            AiNameMatchScore = nameMatchScore;
            AiAuthenticityScore = authenticityScore;
            AiFakeDetectionFlag = fakeDetectionFlag;
            AiOcrExtractedName = ocrExtractedName;
            AiOcrExtractedId = ocrExtractedId;

            ExtractedCountry = extractedCountry;
            ExtractedGovernorate = extractedGovernorate;
            ExtractedCity = extractedCity;
            ExtractedDistrict = extractedDistrict;
            ExtractedAddress = extractedAddress;

            LocationMatchScore = locationMatchScore;
            LocationMismatchFlag = locationMismatchFlag;
            LocationMatchNotes = locationMatchNotes;

            Status = recommendation switch
            {
                "AUTO_VERIFY" => VerificationStatus.AiVerified,
                "MANUAL_REVIEW" => VerificationStatus.RequiresManualReview,
                "REJECT" => VerificationStatus.Rejected,
                _ => VerificationStatus.ManuallyVerified
            };
            CompletedAt = DateTime.UtcNow;
        }

        public static OwnershipVerificationRequest CreatePending(
            Guid propertyId,
            Guid ownerUserId,
            string ownerName,
            string nationalIdBlobPath,
            string ownershipProofBlobPath) =>
            new()
            {
                PropertyId = propertyId,
                OwnerId = ownerUserId,
                OwnerFullName = ownerName,
                NationalIdBlobPath = nationalIdBlobPath,
                OwnershipProofBlobPath = ownershipProofBlobPath,
                Status = VerificationStatus.Pending
            };

        public void MarkFailed(string reason)
        {
            Status = VerificationStatus.Failed;
            FailureReason = reason;
            CompletedAt = DateTime.UtcNow;
        }
    }
}