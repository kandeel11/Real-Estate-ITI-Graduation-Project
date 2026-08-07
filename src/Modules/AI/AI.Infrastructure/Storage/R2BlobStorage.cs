// AI.Infrastructure/Storage/R2BlobStorage.cs
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using AI.Application.Common.Options;
using AI.Application.Interfaces.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AI.Infrastructure.Storage;

public sealed class R2BlobStorage : IBlobStorage
{
    private readonly IAmazonS3 _client;
    private readonly R2Options _options;
    private readonly ILogger<R2BlobStorage> _logger;

    public R2BlobStorage(
        IOptions<R2Options> opts,
        ILogger<R2BlobStorage> logger)
    {
        _options = opts.Value;
        _logger = logger;
        _client = R2ClientFactory.Create(_options);
    }

    public async Task<DocumentDownload> DownloadAsync(
        string blobPath,
        CancellationToken ct = default)
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
                "Downloaded blob: {BlobPath} ({Size} bytes, {ContentType})",
                blobPath, content.Length, contentType);

            return new DocumentDownload(
                Content: content,
                ContentType: contentType,
                FileName: Path.GetFileName(blobPath),
                SizeBytes: content.Length);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new FileNotFoundException($"Blob not found: {blobPath}", ex);
        }
    }

    public Task<string> GetSecureUrlAsync(
        string blobPath,
        TimeSpan? expiry = null,
        CancellationToken ct = default)
    {
        var duration = expiry ?? TimeSpan.FromMinutes(_options.PresignExpiryMinutes > 0 ? _options.PresignExpiryMinutes : 60);

        var secureUrl = _client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = blobPath,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(duration)
        });

        _logger.LogInformation(
            "Generated presigned URL for {BlobPath} | Expires in {Minutes} min",
            blobPath, duration.TotalMinutes);

        return Task.FromResult(secureUrl);
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
