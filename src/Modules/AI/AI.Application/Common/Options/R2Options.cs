// AI.Application/Common/Options/R2Options.cs

using System.ComponentModel.DataAnnotations;

namespace AI.Application.Common.Options;

// Despite the "R2" name (this was originally written for Cloudflare R2), this
// config also works for any S3-compatible provider — Backblaze B2, MinIO, etc.
// Set Endpoint to the provider's full S3-compatible base URL, e.g.:
//   Backblaze B2: https://s3.us-west-002.backblazeb2.com
//   Cloudflare R2: https://{accountId}.r2.cloudflarestorage.com
public sealed class R2Options
{
    public const string SectionName = "R2";

    [Required]
    public string Endpoint { get; init; } = string.Empty;

    /// <summary>Signing region — "auto" for R2, the real region (e.g. "us-west-002") for Backblaze B2.</summary>
    public string Region { get; init; } = "auto";

    [Required]
    public string AccessKeyId { get; init; } = string.Empty;

    [Required]
    public string SecretAccessKey { get; init; } = string.Empty;

    [Required]
    public string BucketName { get; init; } = string.Empty;

    /// <summary>Optional public base URL (r2.dev subdomain or custom domain) for objects served without a presigned link.</summary>
    public string? PublicBaseUrl { get; init; }

    /// <summary>Default presigned-URL expiry in minutes — default 60. R2/S3 caps this at 7 days.</summary>
    public int PresignExpiryMinutes { get; init; } = 60;
}
