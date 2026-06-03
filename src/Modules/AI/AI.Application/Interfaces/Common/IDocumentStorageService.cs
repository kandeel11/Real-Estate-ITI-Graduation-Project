// AI.Application/Interfaces/Common/IDocumentStorageService.cs

namespace AI.Application.Interfaces.Common;

public interface IDocumentStorageService
{
    /// <summary>Uploads file → returns blob path (stored in DB, not the URL)</summary>
    Task<string> UploadAsync(
        string blobFolder,   // e.g. "leases/{leaseId}"
        string fileName,
        string mediaType,
        byte[] content,
        CancellationToken ct = default);

    /// <summary>Generates a time-limited secure SAS URL for a stored blob path</summary>
    Task<string> GetSecureUrlAsync(
        string blobPath,     // path stored in DB
        TimeSpan? expiry = null,   // default 1 hour
        CancellationToken ct = default);

    /// <summary>Deletes a blob by its stored path</summary>
    Task DeleteAsync(
        string blobPath,
        CancellationToken ct = default);
}