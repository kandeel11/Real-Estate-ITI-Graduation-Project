namespace Maintenance.Infrastructure.Settings;

public class AzureStorageSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "maintenance";
}