using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
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
        // Strip directory parts and spaces — keeps blob names clean and avoids encoding issues.
        var safeName = Path.GetFileName(fileName).Replace(" ", "_");
        var blobName = $"leases/{Guid.NewGuid()}_{safeName}";
        var blob = container.GetBlobClient(blobName);
        // Store the real content type so the browser renders (not downloads) the PDF.
        await blob.UploadAsync(fileStream, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = "application/pdf" }
        });
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
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(_sasExpiryMinutes),
            // Override the response headers so the browser downloads the file as a PDF
            // (rather than a blank octet-stream view) — works for blobs uploaded before
            // the content type was set, too.
            ContentType = "application/pdf",
            ContentDisposition = "attachment; filename=\"lease-contract.pdf\""
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);
        var sasToken = sasBuilder.ToSasQueryParameters(credential).ToString();
        return $"https://{_accountName}.blob.core.windows.net/{_containerName}/{blobName}?{sasToken}";
    }
}