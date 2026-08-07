// AI.Infrastructure/Services/R2DocumentStorageService.cs

using AI.Application.Common.Options;
using AI.Application.Interfaces.Common;
using AI.Infrastructure.Storage;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AI.Infrastructure.Services;

/// <summary>
/// Handles all document storage for the platform.
/// Used by: Leases, OwnershipVerification, ContractRag modules.
///
/// Security model:
///   - Files are stored in a private R2 bucket (no public access)
///   - DB stores only the object key (never a URL)
///   - URLs are generated on-demand as time-limited presigned links
///   - Links expire after configured minutes (default 60)
/// </summary>
public sealed class R2DocumentStorageService : IDocumentStorageService
{
    private readonly IAmazonS3 _client;
    private readonly R2Options _options;
    private readonly ILogger<R2DocumentStorageService> _logger;

    public R2DocumentStorageService(
        IOptions<R2Options> options,
        ILogger<R2DocumentStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _client = R2ClientFactory.Create(_options);
    }

    public async Task<string> UploadAsync(
        string blobFolder,
        string fileName,
        string mediaType,
        byte[] content,
        CancellationToken ct = default)
    {
        var safeFileName = SanitizeFileName(fileName);
        var key = $"{blobFolder.TrimEnd('/')}/{safeFileName}";

        using var stream = new MemoryStream(content);

        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = stream,
            ContentType = mediaType,
            DisablePayloadSigning = true
        }, ct);

        _logger.LogInformation(
            "Uploaded object: {Key} ({Size} bytes)", key, content.Length);

        // Return ONLY the object key — never the URL.
        // URL is generated on-demand via GetSecureUrlAsync.
        return key;
    }

    public Task<string> GetSecureUrlAsync(
        string blobPath,
        TimeSpan? expiry = null,
        CancellationToken ct = default)
    {
        var expiryDuration = expiry ?? TimeSpan.FromMinutes(_options.PresignExpiryMinutes);

        var secureUrl = _client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = blobPath,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiryDuration)
        });

        _logger.LogInformation(
            "Generated presigned URL for {BlobPath} | Expires in {Minutes} minutes",
            blobPath, expiryDuration.TotalMinutes);

        return Task.FromResult(secureUrl);
    }

    public async Task<byte[]> DownloadAsync(
        string blobPath,
        CancellationToken ct = default)
    {
        using var response = await _client.GetObjectAsync(_options.BucketName, blobPath, ct);
        using var ms = new MemoryStream();
        await response.ResponseStream.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();

        _logger.LogInformation(
            "Downloaded object: {Key} ({Size} bytes)", blobPath, bytes.Length);

        return bytes;
    }

    public async Task DeleteAsync(
        string blobPath,
        CancellationToken ct = default)
    {
        await _client.DeleteObjectAsync(_options.BucketName, blobPath, ct);
        _logger.LogInformation("Deleted object: {Key}", blobPath);
    }

    private static string SanitizeFileName(string fileName)
    {
        var safe = Path.GetFileNameWithoutExtension(fileName)
            .Replace(" ", "-")
            .ToLowerInvariant();

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        return $"{timestamp}-{safe}{ext}";
    }
}
