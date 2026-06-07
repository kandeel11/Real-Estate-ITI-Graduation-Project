namespace AI.Application.Features.OwnershipVerification.DTOs
{
    /// <summary>
    /// Result from Azure Document Intelligence + Claude text analysis.
    /// </summary>
    public class AiVerificationResult
    {
        /// <summary>Overall confidence score (0-100)</summary>
        public decimal ConfidenceScore { get; set; }

        /// <summary>Name match score between ID and contract (0-100)</summary>
        public decimal NameMatchScore { get; set; }

        /// <summary>Document authenticity score (0-100)</summary>
        public decimal AuthenticityScore { get; set; }

        /// <summary>Flag raised by AI for potential fake documents</summary>
        public bool FakeDetectionFlag { get; set; }

        /// <summary>Owner name extracted from National ID (via Azure DI)</summary>
        public string ExtractedOwnerName { get; set; }

        /// <summary>ID expiry date extracted from National ID (via Azure DI)</summary>
        public string ExtractedIdExpiry { get; set; }

        /// <summary>Recommendation: auto-verify if score >= threshold, else manual review</summary>
        public string Recommendation { get; set; }

        /// <summary>Detailed analysis notes from Claude</summary>
        public string AnalysisNotes { get; set; }
    }
}
