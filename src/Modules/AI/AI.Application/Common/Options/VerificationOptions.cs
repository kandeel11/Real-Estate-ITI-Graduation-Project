namespace AI.Application.Common.Options
{
    /// <summary>
    /// Configuration options for OwnershipVerification feature.
    /// </summary>
    public class VerificationOptions
    {
        public const string SectionName = "Verification";

        public decimal AutoVerifyMinConfidence { get; set; } = 85;
        public decimal RejectMaxConfidence { get; set; } = 40;

        // 🆕 Location thresholds
        public decimal LocationMatchThreshold { get; set; } = 70;   // below → mismatchFlag = true
        public decimal LocationManualReviewThreshold { get; set; } = 70;   // below → force MANUAL_REVIEW
        public decimal LocationRejectThreshold { get; set; } = 40;   // below → force REJECT
        public bool EnableFakeDocumentDetection { get; set; } = true;
    }
}
