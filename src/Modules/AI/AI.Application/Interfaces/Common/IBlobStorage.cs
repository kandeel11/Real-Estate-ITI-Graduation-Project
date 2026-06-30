// AI.Application/Interfaces/Common/IBlobStorage.cs
namespace AI.Application.Interfaces.Common;

public interface IBlobStorage
{
    /// <summary>Downloads a blob and returns its bytes + content type.</summary>
    Task<DocumentDownload> DownloadAsync(
      string blobPath,
      CancellationToken ct = default);

    /// <summary>Generates a short-lived SAS URL for read access to a private blob.</summary>
    Task<string> GetSecureUrlAsync(
        string blobPath,
        TimeSpan? expiry = null,
        CancellationToken ct = default);
}

public sealed record BlobDownloadResult(
    byte[] Content,
    string ContentType,
    string FileName);

public sealed record DocumentDownload(
    byte[] Content,
    string ContentType,
    string FileName,
    long SizeBytes);