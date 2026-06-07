namespace AI.Application.Common.Options
{
    /// <summary>
    /// Configuration options for OwnershipVerification feature.
    /// </summary>
    public class VerificationOptions
    {
        public const string SectionName = "Verification";

        public decimal AutoVerificationThreshold { get; set; } = 80m; // If confidence >= 80%, auto-verify

        public bool EnableFakeDocumentDetection { get; set; } = true;
    }
}
