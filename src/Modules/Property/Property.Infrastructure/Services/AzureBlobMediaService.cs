using AI.Application.Common.Options;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Property.Application.Interfaces;
using static Property.Application.Interfaces.IMediaStorageService;

namespace Property.Infrastructure.Services;

public class AzureBlobMediaService : IMediaStorageService
{
    private readonly BlobContainerClient _container;
    private readonly StorageSharedKeyCredential _credential;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly AzureBlobOptions _options;
    private readonly ILogger<AzureBlobMediaService> _logger;


    public AzureBlobMediaService(BlobServiceClient blobServiceClient, ILogger<AzureBlobMediaService> logger, IOptions<AzureBlobOptions> options)
    {
        _blobServiceClient = blobServiceClient;
        _logger = logger;
        _options = options.Value;
        _credential = new StorageSharedKeyCredential(_options.AccountName, _options.AccountKey);
        _container = _blobServiceClient.GetBlobContainerClient("property-documents");
    }

    public async Task<string> UploadAsync(Guid propertyId, Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        var ContainerName = "property-media";
        var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);

        var blobName = $"{propertyId}/{Guid.CreateVersion7()}-{fileName}";
        var blobClient = containerClient.GetBlobClient(blobName);

        await blobClient.UploadAsync(fileStream, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: ct);

        return blobClient.Uri.ToString();
    }

    public async Task DeleteAsync(string fileUrl, CancellationToken ct = default)
    {
        const string containerName = "property-media";
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

        string blobName;
        if (Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri))
        {
            // Full Azure Blob URL: https://{account}.blob.core.windows.net/{container}/{blob-path}
            // AbsolutePath = "/{container}/{blob-path}" — strip the leading "/{container}/" prefix
            var prefix = $"/{containerName}/";
            var idx = uri.AbsolutePath.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
            blobName = idx >= 0
                ? Uri.UnescapeDataString(uri.AbsolutePath[(idx + prefix.Length)..])
                : Uri.UnescapeDataString(uri.Segments.Last());
        }
        else
        {
            blobName = fileUrl; // already a relative blob path
        }

        await containerClient.DeleteBlobIfExistsAsync(blobName, cancellationToken: ct);
    }


    public async Task<string> UploadAsync(
        Guid OwnerId,
        Stream fileStream,
        string fileName,
        string contentType,
        MediaCategory category,
        CancellationToken ct = default)
    {
        ValidateFile(fileName, contentType, category);

        await _container.CreateIfNotExistsAsync(
            PublicAccessType.None,                   // ✅ private container
            cancellationToken: ct);

        var folder = CategoryFolder(category);
        var safeName = SanitizeFileName(fileName);
        var blobPath = $"{folder}/{OwnerId}/{safeName}";

        var blobClient = _container.GetBlobClient(blobPath);

        // Reset stream if necessary
        if (fileStream.CanSeek && fileStream.Position != 0)
            fileStream.Position = 0;

        await blobClient.UploadAsync(
            fileStream,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType,
                    ContentDisposition = $"inline; filename=\"{safeName}\""
                },
                Metadata = new Dictionary<string, string>
                {
                    ["propertyId"] =OwnerId.ToString(),
                    ["category"] = category.ToString(),
                    ["uploadedAt"] = DateTime.UtcNow.ToString("O")
                }
            },
            cancellationToken: ct);

        _logger.LogInformation(
            "Uploaded {Category} blob: {BlobPath} | PropertyId={PropertyId}",
            category, blobPath, OwnerId);

        return blobPath;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Download (returns bytes + content type, like the AI side)
    // ─────────────────────────────────────────────────────────────────────

    public async Task<MediaDownload> DownloadAsync(
        string blobPath,
        CancellationToken ct = default)
    {
        var blobClient = _container.GetBlobClient(blobPath);

        if (!await blobClient.ExistsAsync(ct))
            throw new FileNotFoundException($"Media blob not found: {blobPath}");

        var propsTask = blobClient.GetPropertiesAsync(cancellationToken: ct);
        var contentTask = blobClient.DownloadContentAsync(ct);
        await Task.WhenAll(propsTask, contentTask);

        var props = propsTask.Result.Value;
        var content = contentTask.Result.Value.Content.ToArray();

        var contentType = !string.IsNullOrWhiteSpace(props.ContentType)
            ? props.ContentType
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

    // ─────────────────────────────────────────────────────────────────────
    // Secure short-lived URL
    // ─────────────────────────────────────────────────────────────────────

    public Task<string> GetSecureUrlAsync(
        string blobPath,
        TimeSpan? expiry = null,
        CancellationToken ct = default)
    {
        var expiryDuration = expiry ?? TimeSpan.FromMinutes(_options.SasExpiryMinutes);
        var blobClient = _container.GetBlobClient(blobPath);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _options.ContainerName,
            BlobName = blobPath,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(expiryDuration),
            Protocol = SasProtocol.Https
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var sasToken = sasBuilder
            .ToSasQueryParameters(_credential)
            .ToString();

        var secureUrl = $"{blobClient.Uri}?{sasToken}";

        _logger.LogInformation(
            "Generated SAS URL for {BlobPath} | Expires in {Minutes} min",
            blobPath, expiryDuration.TotalMinutes);

        return Task.FromResult(secureUrl);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Delete
    // ─────────────────────────────────────────────────────────────────────

    //public async Task DeleteAsync(
    //    string blobPath,
    //    CancellationToken ct = default)
    //{
    //    var blobClient = _container.GetBlobClient(blobPath);
    //    var deleted = await blobClient.DeleteIfExistsAsync(cancellationToken: ct);

    //    if (deleted)
    //        _logger.LogInformation("Deleted media blob: {BlobPath}", blobPath);
    //    else
    //        _logger.LogWarning("Media blob not found for deletion: {BlobPath}", blobPath);
    //}

    // ─────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────

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

        // Strip unsafe chars — keep ASCII alphanumeric, dash, underscore only
        // (non-ASCII letters like Arabic would break HTTP headers)
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

    public async Task<string> UploadAsync(Guid? OwnerId, Stream fileStream, string fileName, string contentType, MediaCategory category, CancellationToken ct = default)
    {
        ValidateFile(fileName, contentType, category);

        await _container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);

        var folder = CategoryFolder(category);
        var safeName = SanitizeFileName(fileName);
        var blobPath = $"{folder}/{OwnerId}/{safeName}";

        var blobClient = _container.GetBlobClient(blobPath);

        // Reset stream if necessary
        if (fileStream.CanSeek && fileStream.Position != 0)
            fileStream.Position = 0;

        await blobClient.UploadAsync(
            fileStream,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType,
                    ContentDisposition = $"inline; filename=\"{safeName}\""
                },
                Metadata = new Dictionary<string, string>
                {
                    ["propertyId"] = OwnerId.ToString(),
                    ["category"] = category.ToString(),
                    ["uploadedAt"] = DateTime.UtcNow.ToString("O")
                }
            },
            cancellationToken: ct);

        _logger.LogInformation(
            "Uploaded {Category} blob: {BlobPath} | PropertyId={PropertyId}",
            category, blobPath, OwnerId);

        return blobPath;
    }

    //public Task<string> UploadAsync(Guid? OwnerId, Stream fileStream, string fileName, string contentType, MediaCategory category, CancellationToken ct = default)
    //{
    //    throw new NotImplementedException();
    //}
}

