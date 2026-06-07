// AI.Infrastructure/Storage/AzureBlobDocumentStorageService.cs

using AI.Application.Common.Options;
using AI.Application.Interfaces.Common;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AI.Infrastructure.Storage;

/// <summary>
/// Handles all document storage for the platform.
/// Used by: Leases, OwnershipVerification, ContractRag modules.
///
/// Security model:
///   - Files are stored in PRIVATE container (no public access)
///   - DB stores only the blob path (never a URL)
///   - URLs are generated on-demand as time-limited SAS tokens
///   - SAS tokens expire after configured minutes (default 60)
/// </summary>
public sealed class AzureBlobDocumentStorageService : IDocumentStorageService
{
    private readonly BlobContainerClient _container;
    private readonly StorageSharedKeyCredential _credential;
    private readonly AzureBlobOptions _options;
    private readonly ILogger<AzureBlobDocumentStorageService> _logger;

    public AzureBlobDocumentStorageService(
        IOptions<AzureBlobOptions> options,
        ILogger<AzureBlobDocumentStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _credential = new StorageSharedKeyCredential(
            _options.AccountName,
            _options.AccountKey);

        var serviceClient = new BlobServiceClient(
            new Uri($"https://{_options.AccountName}.blob.core.windows.net"),
            _credential);

        _container = serviceClient.GetBlobContainerClient(_options.ContainerName);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Upload
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<string> UploadAsync(
        string blobFolder,
        string fileName,
        string mediaType,
        byte[] content,
        CancellationToken ct = default)
    {
        // Ensure container exists as private
        await _container.CreateIfNotExistsAsync(
            PublicAccessType.None,   // ✅ private — no public access
            cancellationToken: ct);

        // Build unique blob path: "leases/3fa85f64/contract.pdf"
        var safeFileName = SanitizeFileName(fileName);
        var blobPath = $"{blobFolder.TrimEnd('/')}/{safeFileName}";

        var blobClient = _container.GetBlobClient(blobPath);

        using var stream = new MemoryStream(content);

        await blobClient.UploadAsync(stream,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = mediaType,
                    ContentDisposition = $"inline; filename=\"{safeFileName}\""
                }
            },
            cancellationToken: ct);

        _logger.LogInformation(
            "Uploaded blob: {BlobPath} ({Size} bytes)", blobPath, content.Length);

        // ✅ Return ONLY the blob path — never the URL
        // URL is generated on-demand via GetSecureUrlAsync
        return blobPath;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Generate Secure SAS URL
    // ─────────────────────────────────────────────────────────────────────────

    public Task<string> GetSecureUrlAsync(
        string blobPath,
        TimeSpan? expiry = null,
        CancellationToken ct = default)
    {
        var expiryDuration = expiry ?? TimeSpan.FromMinutes(_options.SasExpiryMinutes);

        var blobClient = _container.GetBlobClient(blobPath);

        // Build SAS token — read-only, time-limited
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _options.ContainerName,
            BlobName = blobPath,
            Resource = "b",                            // "b" = blob level
            ExpiresOn = DateTimeOffset.UtcNow.Add(expiryDuration),
            Protocol = SasProtocol.Https              // HTTPS only
        };

        // Read permission only — user can view but not modify
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        // Generate SAS token using account key
        var sasToken = sasBuilder.ToSasQueryParameters(_credential).ToString();

        var secureUrl = $"{blobClient.Uri}?{sasToken}";

        _logger.LogInformation(
            "Generated SAS URL for {BlobPath} | Expires in {Minutes} minutes",
            blobPath, expiryDuration.TotalMinutes);

        return Task.FromResult(secureUrl);
    }
    // ─────────────────────────────────────────────────────────────────────────
    // Downlaod
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<byte[]> DownloadAsync(
     string blobPath,
     CancellationToken ct = default)
    {
        var blobClient = _container.GetBlobClient(blobPath);
        var response = await blobClient.DownloadContentAsync(ct);
        var bytes = response.Value.Content.ToArray();

        _logger.LogInformation(
            "Downloaded blob: {BlobPath} ({Size} bytes)", blobPath, bytes.Length);

        return bytes;
    }
    // ─────────────────────────────────────────────────────────────────────────
    // Delete
    // ─────────────────────────────────────────────────────────────────────────

    public async Task DeleteAsync(
        string blobPath,
        CancellationToken ct = default)
    {
        var blobClient = _container.GetBlobClient(blobPath);
        var deleted = await blobClient.DeleteIfExistsAsync(cancellationToken: ct);

        if (deleted)
            _logger.LogInformation("Deleted blob: {BlobPath}", blobPath);
        else
            _logger.LogWarning("Blob not found for deletion: {BlobPath}", blobPath);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static string SanitizeFileName(string fileName)
    {
        // Keep only safe characters, prepend timestamp for uniqueness
        var safe = Path.GetFileNameWithoutExtension(fileName)
            .Replace(" ", "-")
            .ToLowerInvariant();

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        return $"{timestamp}-{safe}{ext}";
    }
}