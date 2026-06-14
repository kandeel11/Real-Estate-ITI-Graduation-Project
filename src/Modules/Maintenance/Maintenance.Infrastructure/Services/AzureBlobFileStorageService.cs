using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Maintenance.Application.Interfaces;
using Maintenance.Infrastructure.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Maintenance.Infrastructure.Services;

public class AzureBlobFileStorageService(IOptions<AzureStorageSettings> options) : IFileStorageService
{
    private readonly string _containerName = options.Value.ContainerName;
    private readonly BlobServiceClient _client = new(options.Value.ConnectionString);

    public async Task<string> UploadAsync(IFormFile file, string folder, CancellationToken ct = default)
    {
        var container = _client.GetBlobContainerClient(_containerName);

        // ← was PublicAccessType.Blob — your account forbids that
        // None = private container, access via SAS URL only
        await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);

        var extension = Path.GetExtension(file.FileName);
        var blobName = $"{folder}/{Guid.NewGuid()}{extension}";
        var blob = container.GetBlobClient(blobName);

        await using var stream = file.OpenReadStream();

        await blob.UploadAsync(stream,
            new BlobHttpHeaders { ContentType = file.ContentType },
            cancellationToken: ct);

        // Generate a SAS URL valid for 1 year — gives read access without making the container public
        var sasUri = blob.GenerateSasUri(
            BlobSasPermissions.Read,
            DateTimeOffset.UtcNow.AddYears(1));

        return sasUri.ToString();
    }

    public async Task DeleteAsync(string fileUrl, CancellationToken ct = default)
    {
        var container = _client.GetBlobContainerClient(_containerName);

        // SAS URL format: https://ACCOUNT.blob.core.windows.net/CONTAINER/folder/file.jpg?sv=...
        // uri.AbsolutePath gives only the path part without the query string (SAS token)
        // Segments: [ "/", "CONTAINER/", "folder/", "file.jpg" ]
        // Skip "/" and "CONTAINER/" → "folder/file.jpg" = the blob name
        var uri = new Uri(fileUrl);
        var blobName = string.Join(string.Empty, uri.Segments.Skip(2));

        var blob = container.GetBlobClient(blobName);
        await blob.DeleteIfExistsAsync(cancellationToken: ct);
    }
}