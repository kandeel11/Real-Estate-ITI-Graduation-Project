// AI.Application/Common/Options/AzureBlobOptions.cs

using System.ComponentModel.DataAnnotations;

namespace AI.Application.Common.Options;

public sealed class AzureBlobOptions
{
    public const string SectionName = "AzureBlob";

    [Required]
    public string AccountName { get; init; } = string.Empty;

    [Required]
    public string AccountKey { get; init; } = string.Empty;

    [Required]
    public string ContainerName { get; init; } = string.Empty;

    /// <summary>Default SAS expiry in minutes — default 60</summary>
    public int SasExpiryMinutes { get; init; } = 60;
}