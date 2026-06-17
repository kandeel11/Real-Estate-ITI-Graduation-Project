namespace Property.Application.Interfaces;

public interface IMediaStorageService
{
     Task<string> UploadAsync(
        Guid? OwnerId,
        Stream fileStream,
        string fileName,
        string contentType,
        MediaCategory category,         // 🆕 photos | documents | ownership
        CancellationToken ct = default);
    public enum MediaCategory
    {
        Photo,
        Document,
        Ownership
    }
    public sealed record MediaDownload(
    byte[] Content,
    string ContentType,
    string FileName,
    long SizeBytes);
    Task<MediaDownload> DownloadAsync(
        string blobPath,
        CancellationToken ct = default);

    Task<string> GetSecureUrlAsync(
        string blobPath,
        TimeSpan? expiry = null,
        CancellationToken ct = default);
    Task<string> UploadAsync(Guid propertyId, Stream fileStream, string fileName, string contentType, CancellationToken ct = default);
    Task DeleteAsync(string fileUrl, CancellationToken ct = default);
}
