namespace Property.Application.Interfaces;

public interface IMediaStorageService
{
    Task<string> UploadAsync(Guid propertyId, Stream fileStream, string fileName, string contentType, CancellationToken ct = default);
    Task DeleteAsync(string fileUrl, CancellationToken ct = default);
}
