using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Property.Application.Interfaces;

namespace Property.Infrastructure.Services;

public class AzureBlobMediaService : IMediaStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private const string ContainerName = "property-media";

    public AzureBlobMediaService(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);

        var blobName = $"{Guid.CreateVersion7()}-{fileName}";
        var blobClient = containerClient.GetBlobClient(blobName);

        await blobClient.UploadAsync(fileStream, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: ct);

        return blobClient.Uri.ToString();
    }

    public async Task DeleteAsync(string fileUrl, CancellationToken ct = default)
    {
        var uri = new Uri(fileUrl);
        var blobName = uri.Segments.Last();
        var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
        await containerClient.DeleteBlobIfExistsAsync(blobName, cancellationToken: ct);
    }
}
