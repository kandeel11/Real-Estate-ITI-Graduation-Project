using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Document.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Document.Infrastructure.Services;

public class AzureBlobService : IAzureBlobService
{
    private readonly BlobServiceClient _client;
    private readonly string _containerName;
    private readonly string _accountName;
    private readonly string _accountKey;
    private readonly int _sasExpiryMinutes;

    public AzureBlobService(IConfiguration config)
    {
        _accountName = config["AzureBlob:AccountName"]!;
        _accountKey = config["AzureBlob:AccountKey"]!;
        _containerName = config["AzureBlob:ContainerName"]!;
        _sasExpiryMinutes = int.Parse(config["AzureBlob:SasExpiryMinutes"] ?? "60");

        var credential = new StorageSharedKeyCredential(_accountName, _accountKey);
        _client = new BlobServiceClient(
            new Uri($"https://{_accountName}.blob.core.windows.net"), credential);
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName)
    {
        var container = _client.GetBlobContainerClient(_containerName);
        await container.CreateIfNotExistsAsync();
        var blobName = $"leases/{Guid.NewGuid()}_{fileName}";
        var blob = container.GetBlobClient(blobName);
        await blob.UploadAsync(fileStream, overwrite: true);
        return blobName; // بنحفظ الـ path مش الـ full URL
    }

    public string GenerateSasUrl(string blobName)
    {
        var credential = new StorageSharedKeyCredential(_accountName, _accountKey);
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerName,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(_sasExpiryMinutes)
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);
        var sasToken = sasBuilder.ToSasQueryParameters(credential).ToString();
        return $"https://{_accountName}.blob.core.windows.net/{_containerName}/{blobName}?{sasToken}";
    }
}