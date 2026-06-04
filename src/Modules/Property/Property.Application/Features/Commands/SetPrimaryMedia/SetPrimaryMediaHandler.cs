using MediatR;
using Property.Application.Interfaces;

namespace Property.Application.Features.Commands.SetPrimaryMedia;

public class SetPrimaryMediaHandler : IRequestHandler<SetPrimaryMediaCommand>
{
    private readonly IPropertyRepository _repository;

    public SetPrimaryMediaHandler(IPropertyRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(SetPrimaryMediaCommand request, CancellationToken cancellationToken)
    {
        var property = await _repository.GetByIdAsync(request.PropertyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Property {request.PropertyId} not found.");

        if (property.OwnerId != request.RequestingUserId)
            throw new UnauthorizedAccessException("Only the property owner can manage media.");

        var allMedia = await _repository.GetMediaAsync(request.PropertyId, cancellationToken);

        foreach (var mediaDto in allMedia)
        {
            var media = await _repository.GetMediaByIdAsync(mediaDto.Id, cancellationToken);
            if (media is null) continue;
            media.SetPrimary(media.Id == request.MediaId);
        }

        await _repository.SaveChangesAsync(cancellationToken);
    }
}
