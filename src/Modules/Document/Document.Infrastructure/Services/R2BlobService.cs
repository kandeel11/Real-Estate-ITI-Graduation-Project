using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Document.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Document.Infrastructure.Services;

public class R2BlobService : IAzureBlobService
{
    private readonly IAmazonS3 _client;
    private readonly string _bucketName;
    private readonly int _presignExpiryMinutes;

    public R2BlobService(IConfiguration config)
    {
        var endpoint = config["R2:Endpoint"]!;
        var region = config["R2:Region"] ?? "auto";
        var accessKeyId = config["R2:AccessKeyId"]!;
        var secretAccessKey = config["R2:SecretAccessKey"]!;
        _bucketName = config["R2:BucketName"]!;
        _presignExpiryMinutes = int.Parse(config["R2:PresignExpiryMinutes"] ?? "60");

        _client = new AmazonS3Client(
            new BasicAWSCredentials(accessKeyId, secretAccessKey),
            new AmazonS3Config
            {
                ServiceURL = endpoint,
                ForcePathStyle = true,
                AuthenticationRegion = region,
                RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED
            });
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName)
    {
        // Strip directory parts and spaces — keeps the key clean and avoids encoding issues.
        var safeName = Path.GetFileName(fileName).Replace(" ", "_");
        var key = $"leases/{Guid.NewGuid()}_{safeName}";

        // Store the real content type so the browser renders (not downloads) the PDF.
        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = fileStream,
            ContentType = "application/pdf",
            DisablePayloadSigning = true
        });

        return key; // بنحفظ الـ path مش الـ full URL
    }

    public string GenerateSasUrl(string blobName)
    {
        return _client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = blobName,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddMinutes(_presignExpiryMinutes),
            // Override the response headers so the browser downloads the file as a PDF
            // (rather than a blank octet-stream view) — works for blobs uploaded before
            // the content type was set, too.
            ResponseHeaderOverrides = new ResponseHeaderOverrides
            {
                ContentType = "application/pdf",
                ContentDisposition = "attachment; filename=\"lease-contract.pdf\""
            }
        });
    }
}
