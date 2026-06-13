using Property.Application.Interfaces;
using static Property.Application.Interfaces.IMediaStorageService;

namespace Property.Infrastructure.Services;

/// <summary>
/// Development fallback when Azure Blob Storage is not configured.
/// Converts uploads to base64 data URLs so they round-trip through the DB
/// and display in the browser without any external storage dependency.
/// </summary>
public class NullMediaStorageService : IMediaStorageService
{
    public async Task<string> UploadAsync(Guid propertyId, Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
        => await ToDataUrlAsync(fileStream, contentType, ct);

    public async Task<string> UploadAsync(Guid? OwnerId, Stream fileStream, string fileName, string contentType, MediaCategory category, CancellationToken ct = default)
        => await ToDataUrlAsync(fileStream, contentType, ct);

    private static async Task<string> ToDataUrlAsync(Stream stream, string contentType, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);
        var base64 = Convert.ToBase64String(ms.ToArray());
        return $"data:{contentType};base64,{base64}";
    }

    public Task<MediaDownload> DownloadAsync(string blobPath, CancellationToken ct = default)
        => throw new NotSupportedException("Media storage is not configured.");

    public Task<string> GetSecureUrlAsync(string blobPath, TimeSpan? expiry = null, CancellationToken ct = default)
        => Task.FromResult(string.Empty);

    public Task DeleteAsync(string fileUrl, CancellationToken ct = default)
        => Task.CompletedTask;
}
