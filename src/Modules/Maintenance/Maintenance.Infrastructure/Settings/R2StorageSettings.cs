namespace Maintenance.Infrastructure.Settings;

public class R2StorageSettings
{
    public string Endpoint { get; set; } = string.Empty;
    public string Region { get; set; } = "auto";
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public int PresignExpiryMinutes { get; set; } = 60;
}
