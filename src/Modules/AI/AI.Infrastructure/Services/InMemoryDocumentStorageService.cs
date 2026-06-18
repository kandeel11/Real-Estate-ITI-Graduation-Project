using AI.Application.Features.OwnershipVerification.Interfaces;
using AI.Application.Interfaces.Common;

namespace AI.Infrastructure.Services
{
    /// <summary>
    /// In-memory document storage service for verification documents.
    /// In production, this would use Azure Blob Storage or similar.
    /// </summary>
    public class InMemoryDocumentStorageService : IDocumentStorageService
    {
        private readonly Dictionary<string, byte[]> _storage = new();

        public Task<string> UploadAsync(string verificationRequestId, string documentType, byte[] fileContent, string mimeType)
        {
            var storagePath = $"verifications/{verificationRequestId}/{documentType}_{Guid.NewGuid()}.bin";
            _storage[storagePath] = fileContent;
            return Task.FromResult(storagePath);
        }

        public Task<byte[]> DownloadAsync(string storagePath)
        {
            if (_storage.TryGetValue(storagePath, out var content))
                return Task.FromResult(content);

            throw new KeyNotFoundException($"Document not found: {storagePath}");
        }

        public Task DeleteAsync(string storagePath)
        {
            _storage.Remove(storagePath);
            return Task.CompletedTask;
        }

        public Task<string> UploadAsync(string blobFolder, string fileName, string mediaType, byte[] content, CancellationToken ct = default)
        {
            var path = $"{blobFolder}/{fileName}";
            _storage[path] = content;
            return Task.FromResult(path);
        }

        public Task<string> GetSecureUrlAsync(string blobPath, TimeSpan? expiry = null, CancellationToken ct = default)
            => Task.FromResult(blobPath);

        public Task DeleteAsync(string blobPath, CancellationToken ct = default)
            => DeleteAsync(blobPath);

        public Task<byte[]> DownloadAsync(string blobPath, CancellationToken ct = default)
            => DownloadAsync(blobPath);
    }
}
