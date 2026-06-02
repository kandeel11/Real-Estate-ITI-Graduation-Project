// AI.Infrastructure/Services/IndexManagementService.cs

using AI.Application.Interfaces.Common;
using Microsoft.Extensions.Logging;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace AI.Infrastructure.Services;

public sealed class IndexManagementService : IIndexManagementService
{
    private readonly QdrantClient _qdrant;
    private readonly ILogger<IndexManagementService> _logger;

    public IndexManagementService(
        QdrantClient qdrant,
        ILogger<IndexManagementService> logger)
    {
        _qdrant = qdrant;
        _logger = logger;
    }

    public async Task EnsureIndexesAsync(
        string collectionName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Creating payload indexes on '{Collection}'", collectionName);

        await _qdrant.CreatePayloadIndexAsync(
            collectionName, "contractId",
            PayloadSchemaType.Keyword,
            cancellationToken: cancellationToken);

        await _qdrant.CreatePayloadIndexAsync(
            collectionName, "pageNumber",
            PayloadSchemaType.Integer,
            cancellationToken: cancellationToken);

        await _qdrant.CreatePayloadIndexAsync(
            collectionName, "chunkIndex",
            PayloadSchemaType.Integer,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Payload indexes ready on '{Collection}'", collectionName);
    }
}