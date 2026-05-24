using MediatR;
using Property.Application.Interfaces;

namespace Property.Application.Features.Commands.RemovePropertyMedia;

public class RemovePropertyMediaHandler : IRequestHandler<RemovePropertyMediaCommand>
{
    private readonly IPropertyRepository _repository;
    private readonly IMediaStorageService _storageService;

    public RemovePropertyMediaHandler(IPropertyRepository repository, IMediaStorageService storageService)
    {
        _repository = repository;
        _storageService = storageService;
    }

    public async Task Handle(RemovePropertyMediaCommand request, CancellationToken cancellationToken)
    {
        var property = await _repository.GetByIdAsync(request.PropertyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Property {request.PropertyId} not found.");

        if (property.OwnerId != request.RequestingUserId)
            throw new UnauthorizedAccessException("Only the property owner can remove media.");

        var media = await _repository.GetMediaByIdAsync(request.MediaId, cancellationToken)
            ?? throw new KeyNotFoundException($"Media {request.MediaId} not found.");

        await _storageService.DeleteAsync(media.MediaUrl, cancellationToken);
        await _repository.RemoveMediaAsync(media, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
