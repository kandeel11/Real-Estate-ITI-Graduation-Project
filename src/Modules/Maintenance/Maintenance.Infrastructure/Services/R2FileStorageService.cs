using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Maintenance.Application.Interfaces;
using Maintenance.Infrastructure.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Maintenance.Infrastructure.Services;

public class R2FileStorageService : IFileStorageService
{
    private readonly IAmazonS3 _client;
    private readonly string _bucketName;

    public R2FileStorageService(IOptions<R2StorageSettings> options)
    {
        var o = options.Value;
        _bucketName = o.BucketName;
        _client = new AmazonS3Client(
            new BasicAWSCredentials(o.AccessKeyId, o.SecretAccessKey),
            new AmazonS3Config
            {
                ServiceURL = o.Endpoint,
                ForcePathStyle = true,
                AuthenticationRegion = o.Region,
                RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED
            });
    }

    public async Task<string> UploadAsync(IFormFile file, string folder, CancellationToken ct = default)
    {
        var extension = Path.GetExtension(file.FileName);
        var key = $"{folder}/{Guid.NewGuid()}{extension}";

        await using var stream = file.OpenReadStream();

        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = stream,
            ContentType = file.ContentType,
            DisablePayloadSigning = true
        }, ct);

        // Presigned GET, valid for the S3/R2 signature v4 maximum (7 days).
        // NB: Azure's SAS could be issued for a full year — R2 can't match that,
        // so links stored from this call go stale after 7 days and need
        // regenerating from the stored key if long-lived access is needed later.
        var url = _client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddDays(7)
        });

        return url;
    }

    public async Task DeleteAsync(string fileUrl, CancellationToken ct = default)
    {
        // Presigned URL format: https://ACCOUNTID.r2.cloudflarestorage.com/BUCKET/folder/file.jpg?X-Amz-...
        // uri.AbsolutePath gives only the path part without the query string (signature)
        // Segments: [ "/", "BUCKET/", "folder/", "file.jpg" ]
        // Skip "/" and "BUCKET/" → "folder/file.jpg" = the object key
        var uri = new Uri(fileUrl);
        var key = string.Join(string.Empty, uri.Segments.Skip(2));

        await _client.DeleteObjectAsync(_bucketName, key, ct);
    }
}
