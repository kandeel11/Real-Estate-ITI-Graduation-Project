using Property.Application.Interfaces;
using static Property.Application.Interfaces.IMediaStorageService;

namespace Property.Infrastructure.Services;

public class NullMediaStorageService : IMediaStorageService
{
    public Task<string> UploadAsync(Guid propertyId, Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
        => Task.FromResult(string.Empty);

    public Task<string> UploadAsync(Guid? OwnerId, Stream fileStream, string fileName, string contentType, MediaCategory category, CancellationToken ct = default)
        => Task.FromResult(string.Empty);

    public Task<MediaDownload> DownloadAsync(string blobPath, CancellationToken ct = default)
        => throw new NotSupportedException("Media storage is not configured.");

    public Task<string> GetSecureUrlAsync(string blobPath, TimeSpan? expiry = null, CancellationToken ct = default)
        => Task.FromResult(string.Empty);

    public Task DeleteAsync(string fileUrl, CancellationToken ct = default)
        => Task.CompletedTask;
}
