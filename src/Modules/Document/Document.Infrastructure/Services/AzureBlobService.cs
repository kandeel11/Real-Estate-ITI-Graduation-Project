using Azure.Storage;
using Azure.Storage.Blobs;
using Document.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Document.Infrastructure.Services;

public class AzureBlobService : IAzureBlobService
{
    private readonly BlobServiceClient _client;
    private readonly string _containerName;

    public AzureBlobService(IConfiguration config)
    {
        var accountName = config["AzureBlob:AccountName"];
        var accountKey = config["AzureBlob:AccountKey"];
        _containerName = config["AzureBlob:ContainerName"]!;
        var credential = new StorageSharedKeyCredential(accountName, accountKey);
        _client = new BlobServiceClient(
            new Uri($"https://{accountName}.blob.core.windows.net"), credential);
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName)
    {
        var container = _client.GetBlobContainerClient(_containerName);
        await container.CreateIfNotExistsAsync();
        var blobName = $"leases/{Guid.NewGuid()}_{fileName}";
        var blob = container.GetBlobClient(blobName);
        await blob.UploadAsync(fileStream, overwrite: true);
        return blob.Uri.ToString();
    }
}