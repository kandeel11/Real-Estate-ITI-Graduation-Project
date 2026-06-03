namespace AI.Application.Features.OwnershipVerification.DTOs
{
    /// <summary>
    /// Request to analyze ownership documents (National ID + Proof of Ownership).
    /// Uses byte[] instead of base64 for efficiency.
    /// </summary>
    public class DocumentAnalysisRequest
    {
        /// <summary>Document type (e.g., NationalID, OwnershipProof)</summary>
        public string DocumentType { get; set; }

        /// <summary>Raw document bytes</summary>
        public byte[] FileContent { get; set; }

        /// <summary>MIME type of the file (image/jpeg, application/pdf, etc.)</summary>
        public string MimeType { get; set; }
    }
}
