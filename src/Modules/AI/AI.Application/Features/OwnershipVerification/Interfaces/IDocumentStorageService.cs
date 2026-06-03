namespace AI.Application.Features.OwnershipVerification.Interfaces
{
    /// <summary>
    /// Service for storing and retrieving verification documents (National ID, Proof of Ownership).
    /// </summary>
    public interface IDocumentStorageService
    {
        /// <summary>
        /// Upload a verification document to storage.
        /// </summary>
        Task<string> UploadAsync(string verificationRequestId, string documentType, byte[] fileContent, string mimeType);

        /// <summary>
        /// Download a verification document from storage.
        /// </summary>
        Task<byte[]> DownloadAsync(string storagePath);

        /// <summary>
        /// Delete a verification document from storage.
        /// </summary>
        Task DeleteAsync(string storagePath);
    }
}
