// AI.Infrastructure/Storage/R2ClientFactory.cs
using Amazon.Runtime;
using Amazon.S3;
using AI.Application.Common.Options;

namespace AI.Infrastructure.Storage;

/// <summary>
/// Builds an S3 client pointed at an S3-compatible endpoint (Backblaze B2,
/// Cloudflare R2, MinIO, ...). Path-style addressing and relaxed checksum
/// handling are needed for compatibility — the AWS SDK's newer default
/// (always-on trailer checksums) breaks uploads against most non-AWS S3
/// implementations if left on.
/// </summary>
internal static class R2ClientFactory
{
    public static IAmazonS3 Create(R2Options options)
    {
        var config = new AmazonS3Config
        {
            ServiceURL = options.Endpoint,
            ForcePathStyle = true,
            AuthenticationRegion = options.Region,
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED
        };

        return new AmazonS3Client(
            new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey),
            config);
    }
}
