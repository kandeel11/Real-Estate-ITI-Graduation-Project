using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
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

        // Creates the container if it doesn't exist yet and makes blobs publicly readable
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);

        var extension = Path.GetExtension(file.FileName);
        var blobName = $"{folder}/{Guid.NewGuid()}{extension}";
        var blob = container.GetBlobClient(blobName);

        await using var stream = file.OpenReadStream();

        await blob.UploadAsync(stream,
            new BlobHttpHeaders { ContentType = file.ContentType },
            cancellationToken: ct);

        // Returns the full public URL e.g.
        // https://ACCOUNT.blob.core.windows.net/maintenance/maintenance/uuid.jpg
        return blob.Uri.ToString();
    }

    public async Task DeleteAsync(string fileUrl, CancellationToken ct = default)
    {
        var container = _client.GetBlobContainerClient(_containerName);

        // Extract the blob name from the full URL.
        // URL format: https://ACCOUNT.blob.core.windows.net/CONTAINER/folder/filename
        // uri.Segments = [ "/", "CONTAINER/", "folder/", "filename" ]
        // We skip "/" and "CONTAINER/" → join the rest → "folder/filename"
        var uri = new Uri(fileUrl);
        var blobName = string.Join(string.Empty, uri.Segments.Skip(2));

        var blob = container.GetBlobClient(blobName);
        await blob.DeleteIfExistsAsync(cancellationToken: ct);
    }
}