namespace AI.Application.Features.OwnershipVerification.DTOs
{
    /// <summary>
    /// DTO for API responses containing verification status and extracted data.
    /// </summary>
    public class VerificationStatusDto
    {
        public Guid VerificationRequestId { get; set; }

        public string Status { get; set; } // Pending, AiVerified, RequiresManualReview, Rejected

        public decimal? AiConfidenceScore { get; set; }

        public string? AiOcrExtractedOwnerName { get; set; }

        public string? AiOcrExtractedIdExpiry { get; set; }

        public bool AiFakeDetectionFlag { get; set; }

        public string? AdminReviewNotes { get; set; }

        public DateTime? AdminReviewedAt { get; set; }

        public string? RejectionReason { get; set; }
    }
}
