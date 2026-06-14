using MediatR;
using Property.Application.Interfaces;
using Property.Domain.Entities;
using RealEstate.Shared.Kernel.Shared.Contracts;

namespace Property.Application.Features.Commands.AddPropertyMedia;

public class AddPropertyMediaHandler : IRequestHandler<AddPropertyMediaCommand, Guid>
{
    private readonly IPropertyRepository _repository;
    private readonly IMediaStorageService _storageService;
    private readonly ISubscriptionLimitChecker _limitChecker;

    public AddPropertyMediaHandler(
        IPropertyRepository repository,
        IMediaStorageService storageService,
        ISubscriptionLimitChecker limitChecker)
    {
        _repository = repository;
        _storageService = storageService;
        _limitChecker = limitChecker;
    }

    public async Task<Guid> Handle(AddPropertyMediaCommand request, CancellationToken cancellationToken)
    {
        var property = await _repository.GetByIdAsync(request.PropertyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Property {request.PropertyId} not found.");

        if (property.OwnerId != request.RequestingUserId)
            throw new UnauthorizedAccessException("Only the property owner can upload media.");

        // Enforce the plan's storage cap before uploading bytes
        var fileGb = request.FileSizeKb / 1024m / 1024m;
        var (allowed, reason) = await _limitChecker.CheckStorageLimitAsync(
            property.OwnerId, property.CompanyId, fileGb, cancellationToken);
        if (!allowed)
            throw new InvalidOperationException(reason);

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

        // Count the bytes against the plan's storage quota
        await _limitChecker.AddStorageUsageAsync(property.OwnerId, property.CompanyId, fileGb, cancellationToken);

        return media.Id;
    }
}
