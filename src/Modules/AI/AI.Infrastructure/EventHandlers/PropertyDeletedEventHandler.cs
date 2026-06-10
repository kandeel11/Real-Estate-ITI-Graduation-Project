using MediatR;
using Microsoft.Extensions.Logging;
using Qdrant.Client;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;

namespace AI.Infrastructure.EventHandlers;

/// <summary>
/// When a property is deleted, remove its vector from the Qdrant "properties" collection
/// (the one used for duplicate detection). Points are stored with the property id as the
/// point id, so we delete by that id.
/// </summary>
public sealed class PropertyDeletedEventHandler : INotificationHandler<PropertyDeletedEvent>
{
    private const string CollectionName = "properties";

    private readonly QdrantClient _qdrant;
    private readonly ILogger<PropertyDeletedEventHandler> _logger;

    public PropertyDeletedEventHandler(QdrantClient qdrant, ILogger<PropertyDeletedEventHandler> logger)
    {
        _qdrant = qdrant;
        _logger = logger;
    }

    public async Task Handle(PropertyDeletedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            // Nothing to delete if the collection was never created (no duplicate checks run yet).
            if (!await _qdrant.CollectionExistsAsync(CollectionName, cancellationToken))
                return;

            await _qdrant.DeleteAsync(CollectionName, notification.PropertyId, cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Removed property {PropertyId} from Qdrant collection '{Collection}'.",
                notification.PropertyId, CollectionName);
        }
        catch (Exception ex)
        {
            // Non-critical: vector-store cleanup failure shouldn't fail the deletion flow.
            _logger.LogError(ex,
                "Failed to remove property {PropertyId} from Qdrant.", notification.PropertyId);
        }
    }
}
