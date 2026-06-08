using MediatR;
using Property.Application.Interfaces;
using Property.Domain.Entities;

namespace Property.Application.Features.Commands.AddPropertyMedia;

public class AddPropertyMediaHandler : IRequestHandler<AddPropertyMediaCommand, Guid>
{
    private readonly IPropertyRepository _repository;
    private readonly IMediaStorageService _storageService;

    public AddPropertyMediaHandler(IPropertyRepository repository, IMediaStorageService storageService)
    {
        _repository = repository;
        _storageService = storageService;
    }

    public async Task<Guid> Handle(AddPropertyMediaCommand request, CancellationToken cancellationToken)
    {
        var property = await _repository.GetByIdAsync(request.PropertyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Property {request.PropertyId} not found.");

        if (property.OwnerId != request.RequestingUserId)
            throw new UnauthorizedAccessException("Only the property owner can upload media.");

        var mediaUrl = await _storageService.UploadAsync(
            request.PropertyId, request.FileStream, request.FileName, request.ContentType, cancellationToken);

        var media = PropertyMedia.Create(
            request.PropertyId,
            mediaUrl,
            request.MediaType,
            request.FileSizeKb,
            request.SortOrder);

        await _repository.AddMediaAsync(media, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return media.Id;
    }
}
