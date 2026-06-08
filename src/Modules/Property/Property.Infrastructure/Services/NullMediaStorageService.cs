using Property.Application.Interfaces;

namespace Property.Infrastructure.Services;

public class NullMediaStorageService : IMediaStorageService
{
    public Task<string> UploadAsync(Guid propertyId, Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
        => throw new InvalidOperationException("Azure Blob Storage is not configured. Set 'ConnectionStrings:AzureBlobStorage' in appsettings.");

    public Task DeleteAsync(string fileUrl, CancellationToken ct = default)
        => throw new InvalidOperationException("Azure Blob Storage is not configured. Set 'ConnectionStrings:AzureBlobStorage' in appsettings.");
}
