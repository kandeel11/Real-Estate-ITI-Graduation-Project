// AI.Infrastructure/Storage/AzureBlobStorage.cs
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage;
using Azure.Storage.Blobs;
using AI.Application.Common.Options;
using AI.Application.Interfaces.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AI.Infrastructure.Storage;

public sealed class AzureBlobStorage : IBlobStorage
{
    private readonly BlobContainerClient _container;
    private readonly ILogger<AzureBlobStorage> _logger;

    public AzureBlobStorage(
        IOptions<AzureBlobOptions> opts,
        ILogger<AzureBlobStorage> logger)
    {
        var o = opts.Value;

        var credential = new StorageSharedKeyCredential(o.AccountName, o.AccountKey);

        var blobUri = new Uri($"https://{o.AccountName}.blob.core.windows.net");

        var service = new BlobServiceClient(blobUri, credential);

        _container = service.GetBlobContainerClient("property-documents");
        _logger = logger;
    }

    public async Task<DocumentDownload> DownloadAsync(
    string blobPath,
    CancellationToken ct = default)
    {
        var blobClient = _container.GetBlobClient(blobPath);

        if (!await blobClient.ExistsAsync(ct))
            throw new FileNotFoundException($"Blob not found: {blobPath}");

        // Fetch metadata (Content-Type) + content in parallel
        var propsTask = blobClient.GetPropertiesAsync(cancellationToken: ct);
        var contentTask = blobClient.DownloadContentAsync(ct);
        await Task.WhenAll(propsTask, contentTask);

        var props = propsTask.Result.Value;
        var content = contentTask.Result.Value.Content.ToArray();

        var contentType = !string.IsNullOrWhiteSpace(props.ContentType)
            ? props.ContentType
            : InferMimeType(blobPath);

        _logger.LogInformation(
            "Downloaded blob: {BlobPath} ({Size} bytes, {ContentType})",
            blobPath, content.Length, contentType);

        return new DocumentDownload(
            Content: content,
            ContentType: contentType,
            FileName: Path.GetFileName(blobPath),
            SizeBytes: content.Length);
    }

    // Fallback if the blob has no Content-Type metadata
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