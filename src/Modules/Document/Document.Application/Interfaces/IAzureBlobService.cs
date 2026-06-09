namespace Document.Application.Interfaces;

public interface IAzureBlobService
{
    Task<string> UploadAsync(Stream fileStream, string fileName);
    string GenerateSasUrl(string blobName);
}