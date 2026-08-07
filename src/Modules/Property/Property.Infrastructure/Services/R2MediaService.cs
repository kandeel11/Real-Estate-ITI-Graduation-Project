using AI.Application.Common.Options;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Property.Application.Interfaces;
using static Property.Application.Interfaces.IMediaStorageService;

namespace Property.Infrastructure.Services;

public class R2MediaService : IMediaStorageService
{
    private const string MediaBucketFolder = "property-media";

    private readonly IAmazonS3 _client;
    private readonly R2Options _options;
    private readonly ILogger<R2MediaService> _logger;

    public R2MediaService(IAmazonS3 client, ILogger<R2MediaService> logger, IOptions<R2Options> options)
    {
        _client = client;
        _logger = logger;
        _options = options.Value;
    }

    // Quick upload for plain property photos — rendered directly, no presigning.
    // Requires the R2 bucket's public access (r2.dev subdomain or custom domain)
    // to be enabled and R2:PublicBaseUrl configured; otherwise falls back to a
    // 7-day presigned URL (the S3/R2 signature v4 maximum).
    public async Task<string> UploadAsync(Guid propertyId, Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        var key = $"{MediaBucketFolder}/{propertyId}/{Guid.CreateVersion7()}-{fileName}";

        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = fileStream,
            ContentType = contentType,
            DisablePayloadSigning = true
        }, ct);

        return BuildDirectUrl(key);
    }

    public async Task DeleteAsync(string fileUrl, CancellationToken ct = default)
    {
        var key = ExtractKey(fileUrl);
        await _client.DeleteObjectAsync(_options.BucketName, key, ct);
    }

    public async Task<string> UploadAsync(
        Guid? OwnerId,
        Stream fileStream,
        string fileName,
        string contentType,
        MediaCategory category,
        CancellationToken ct = default)
    {
        ValidateFile(fileName, contentType, category);

        var folder = CategoryFolder(category);
        var safeName = SanitizeFileName(fileName);
        var key = $"{folder}/{OwnerId}/{safeName}";

        if (fileStream.CanSeek && fileStream.Position != 0)
            fileStream.Position = 0;

        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = fileStream,
            ContentType = contentType,
            DisablePayloadSigning = true,
            Metadata =
            {
                ["propertyId"] = OwnerId.ToString() ?? string.Empty,
                ["category"] = category.ToString(),
                ["uploadedAt"] = DateTime.UtcNow.ToString("O")
            }
        }, ct);

        _logger.LogInformation(
            "Uploaded {Category} object: {Key} | OwnerId={OwnerId}",
            category, key, OwnerId);

        return key;
    }

    public async Task<MediaDownload> DownloadAsync(string blobPath, CancellationToken ct = default)
    {
        try
        {
            using var response = await _client.GetObjectAsync(_options.BucketName, blobPath, ct);
            using var ms = new MemoryStream();
            await response.ResponseStream.CopyToAsync(ms, ct);
            var content = ms.ToArray();

            var contentType = !string.IsNullOrWhiteSpace(response.Headers.ContentType)
                ? response.Headers.ContentType
                : InferMimeType(blobPath);

            _logger.LogInformation(
                "Downloaded media: {BlobPath} ({Size} bytes, {ContentType})",
                blobPath, content.Length, contentType);

            return new MediaDownload(
                Content: content,
                ContentType: contentType,
                FileName: Path.GetFileName(blobPath),
                SizeBytes: content.Length);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new FileNotFoundException($"Media object not found: {blobPath}", ex);
        }
    }

    public Task<string> GetSecureUrlAsync(string blobPath, TimeSpan? expiry = null, CancellationToken ct = default)
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
            "Generated presigned URL for {BlobPath} | Expires in {Minutes} min",
            blobPath, expiryDuration.TotalMinutes);

        return Task.FromResult(secureUrl);
    }

    // ── helpers ──────────────────────────────────────────────────────────

    private string BuildDirectUrl(string key)
    {
        if (!string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
            return $"{_options.PublicBaseUrl.TrimEnd('/')}/{key}";

        _logger.LogWarning(
            "R2:PublicBaseUrl is not configured — falling back to a 7-day presigned URL for {Key}. " +
            "Enable public access on the R2 bucket and set R2:PublicBaseUrl to avoid links expiring.",
            key);

        return _client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddDays(7)
        });
    }

    private string ExtractKey(string fileUrl)
    {
        if (!Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri))
            return fileUrl; // already a relative key

        // Public URL: {PublicBaseUrl}/{key} — AbsolutePath is "/{key}"
        // Presigned URL: same shape, query string carries the signature
        return Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
    }

    private static readonly HashSet<string> ImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/bmp", "image/tiff", "image/heic"
    };

    private static readonly HashSet<string> DocumentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg", "image/png", "image/bmp", "image/tiff", "image/heic", "image/heif"
    };

    private static void ValidateFile(string fileName, string contentType, MediaCategory category)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.");

        var allowed = category switch
        {
            MediaCategory.Photo => ImageTypes,
            MediaCategory.Document => DocumentTypes,
            MediaCategory.Ownership => DocumentTypes,
            _ => throw new ArgumentOutOfRangeException(nameof(category))
        };

        if (!allowed.Contains(contentType))
            throw new NotSupportedException(
                $"Content type '{contentType}' is not allowed for {category}. " +
                $"Allowed: {string.Join(", ", allowed)}");
    }

    private static string CategoryFolder(MediaCategory category) => category switch
    {
        MediaCategory.Photo => "photos",
        MediaCategory.Document => "documents",
        MediaCategory.Ownership => "ownership",
        _ => "misc"
    };

    private static string SanitizeFileName(string fileName)
    {
        var stem = Path.GetFileNameWithoutExtension(fileName)
            .Replace(" ", "-")
            .ToLowerInvariant();

        stem = new string(stem.Where(c =>
            char.IsAsciiLetterOrDigit(c) || c == '-' || c == '_').ToArray());

        if (string.IsNullOrWhiteSpace(stem)) stem = "file";

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        return $"{timestamp}-{stem}{ext}";
    }

    private static string InferMimeType(string blobPath)
    {
        var ext = Path.GetExtension(blobPath).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".tif" => "image/tiff",
            ".tiff" => "image/tiff",
            ".heic" => "image/heic",
            ".heif" => "image/heif",
            ".bmp" => "image/bmp",
            _ => "application/octet-stream"
        };
    }
}
