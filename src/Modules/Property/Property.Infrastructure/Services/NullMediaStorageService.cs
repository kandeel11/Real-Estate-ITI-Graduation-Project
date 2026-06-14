using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Property.Application.Interfaces;
using static Property.Application.Interfaces.IMediaStorageService;

namespace Property.Infrastructure.Services;

/// <summary>
/// Development fallback when Azure Blob Storage is not configured.
/// Saves uploaded files to wwwroot/uploads/ and returns an absolute URL
/// (e.g. http://localhost:5000/uploads/properties/{id}/{guid}.jpg).
/// This keeps MediaUrl short so it fits inside the varchar(2000) column.
/// </summary>
public class NullMediaStorageService : IMediaStorageService
{
    private readonly IWebHostEnvironment _env;
    private readonly IHttpContextAccessor _http;

    public NullMediaStorageService(IWebHostEnvironment env, IHttpContextAccessor http)
    {
        _env = env;
        _http = http;
    }

    public async Task<string> UploadAsync(
        Guid propertyId,
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken ct = default)
    {
        var relativePath = BuildRelativePath($"uploads/properties/{propertyId}", fileName);
        return await SaveAndReturnUrlAsync(fileStream, relativePath, ct);
    }

    public async Task<string> UploadAsync(
        Guid? ownerId,
        Stream fileStream,
        string fileName,
        string contentType,
        MediaCategory category,
        CancellationToken ct = default)
    {
        var folder = $"uploads/{category.ToString().ToLowerInvariant()}/{ownerId}";
        var relativePath = BuildRelativePath(folder, fileName);
        return await SaveAndReturnUrlAsync(fileStream, relativePath, ct);
    }

    public Task<MediaDownload> DownloadAsync(string blobPath, CancellationToken ct = default)
        => throw new NotSupportedException("Use the static file URL directly — Azure Storage is not configured.");

    public Task<string> GetSecureUrlAsync(string blobPath, TimeSpan? expiry = null, CancellationToken ct = default)
        => Task.FromResult(blobPath);

    public Task DeleteAsync(string fileUrl, CancellationToken ct = default)
    {
        try
        {
            if (Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri))
            {
                var root = WebRootPath();
                var local = Path.Combine(root, uri.AbsolutePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(local))
                    File.Delete(local);
            }
        }
        catch { }
        return Task.CompletedTask;
    }

    // ── helpers ────────────────────────────────────────────────────────────

    private static string BuildRelativePath(string folder, string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return $"{folder}/{Guid.NewGuid()}{ext}";
    }

    private async Task<string> SaveAndReturnUrlAsync(Stream fileStream, string relativePath, CancellationToken ct)
    {
        var root = WebRootPath();
        var fullPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var fs = File.Create(fullPath);
        await fileStream.CopyToAsync(fs, ct);

        var request = _http.HttpContext?.Request;
        if (request is not null)
            return $"{request.Scheme}://{request.Host}/{relativePath}";

        return $"/{relativePath}";
    }

    private string WebRootPath()
        => !string.IsNullOrEmpty(_env.WebRootPath)
            ? _env.WebRootPath
            : Path.Combine(_env.ContentRootPath, "wwwroot");
}
