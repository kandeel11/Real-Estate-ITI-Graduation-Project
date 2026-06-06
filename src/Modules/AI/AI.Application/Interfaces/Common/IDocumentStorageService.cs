// AI.Application/Interfaces/Common/IDocumentStorageService.cs

public interface IDocumentStorageService
{
    Task<string> UploadAsync(
        string blobFolder, string fileName,
        string mediaType, byte[] content,
        CancellationToken ct = default);

    Task<string> GetSecureUrlAsync(
        string blobPath, TimeSpan? expiry = null,
        CancellationToken ct = default);

    // ✅ Add this — Mohamed needs it to download PDF before processing
    Task<byte[]> DownloadAsync(
        string blobPath,
        CancellationToken ct = default);

    Task DeleteAsync(
        string blobPath,
        CancellationToken ct = default);
}