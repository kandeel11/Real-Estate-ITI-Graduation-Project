using AI.Domain.Enums;
using Domain.Entities;

namespace AI.Domain.Entities
{
    public class OwnershipVerificationRequest : BaseEntity
    {
        public Guid PropertyId { get; set; } // ref property.properties 
        public Guid OwnerId { get; set; } // ref identity.users 

        public VerificationStatus Status { get; set; } = VerificationStatus.Pending;

        public decimal? AiConfidenceScore { get; set; }
        public decimal? AiNameMatchScore { get; set; }
        public decimal? AiAuthenticityScore { get; set; }
        public bool AiFakeDetectionFlag { get; set; } = false;
        public string? AiOcrExtractedName { get; set; }
        public string? AiOcrExtractedId { get; set; }

        // NEW: From Azure Document Intelligence extraction
        public string? AiOcrExtractedIdExpiry { get; set; }
        public string? AiOcrExtractedOwnerName { get; set; }

        public Guid? AdminReviewerId { get; set; }
        public string? AdminReviewNotes { get; set; }
        public DateTime? AdminReviewedAt { get; set; }
        public string? RejectionReason { get; set; }
    }
}
