using AI.Application.Features.OwnershipVerification.DTOs;

namespace AI.Application.Features.OwnershipVerification.Interfaces
{
    /// <summary>
    /// Service for AI-driven ownership verification using Azure Document Intelligence + Claude.
    /// Step 1: Azure DI prebuilt-idDocument extracts structured fields from National ID
    /// Step 2: Azure DI prebuilt-layout extracts text from ownership proof/contract
    /// Step 3: Claude text model compares names and verifies authenticity
    /// </summary>
    public interface IOwnershipAiService
    {
        /// <summary>
        /// Analyze ownership documents and return AI verification result.
        /// </summary>
        Task<AiVerificationResult> AnalyzeAsync(
       byte[] nationalIdFile,
       string nationalIdMimeType,
       byte[] ownershipProofFile,
       string ownershipProofMimeType,
       string ownerName,
       PropertyLocationDto expectedLocation,    // 🆕
       CancellationToken ct = default);
    }
}
