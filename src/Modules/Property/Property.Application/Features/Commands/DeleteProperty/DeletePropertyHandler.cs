using MediatR;
using Microsoft.Extensions.Logging;
using Property.Application.Interfaces;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;

namespace Property.Application.Features.Commands.DeleteProperty;

public class DeletePropertyHandler : IRequestHandler<DeletePropertyCommand>
{
    private readonly IPropertyRepository _repository;
    private readonly IMediaStorageService _storage;
    private readonly IPublisher _publisher;
    private readonly ILogger<DeletePropertyHandler> _logger;

    public DeletePropertyHandler(
        IPropertyRepository repository,
        IMediaStorageService storage,
        IPublisher publisher,
        ILogger<DeletePropertyHandler> logger)
    {
        _repository = repository;
        _storage = storage;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task Handle(DeletePropertyCommand request, CancellationToken cancellationToken)
    {
        var property = await _repository.GetByIdAsync(request.PropertyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Property {request.PropertyId} not found.");

        if (property.OwnerId != request.RequestingUserId)
            throw new UnauthorizedAccessException("Only the property owner can delete this listing.");

        // 1) Remove the property's media (all photos) from Azure Blob Storage + their rows.
        var media = await _repository.GetMediaAsync(request.PropertyId, cancellationToken);
        foreach (var item in media)
        {
            try
            {
                await _storage.DeleteAsync(item.MediaUrl, cancellationToken);
            }
            catch (Exception ex)
            {
                // Non-critical: a missing/failed blob delete shouldn't block the property deletion.
                _logger.LogError(ex, "Failed to delete media blob {MediaUrl} for property {PropertyId}.",
                    item.MediaUrl, request.PropertyId);
            }

            var entity = await _repository.GetMediaByIdAsync(item.Id, cancellationToken);
            if (entity is not null)
                await _repository.RemoveMediaAsync(entity, cancellationToken);
        }

        // 2) Soft-delete the property itself.
        property.SoftDelete();
        await _repository.SaveChangesAsync(cancellationToken);

        // 3) Tell other modules to clean up — AI removes the property's Qdrant vector.
        await _publisher.Publish(new PropertyDeletedEvent(request.PropertyId), cancellationToken);
    }
}
