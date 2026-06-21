using MediatR;
using Property.Application.Interfaces;
using Property.Domain.Entities;
using RealEstate.Shared.Kernel.Shared.Contracts;

namespace Property.Application.Features.Commands.SaveProperty;

public class SavePropertyHandler : IRequestHandler<SavePropertyCommand>
{
    private readonly ISavedPropertyRepository _savedRepo;
    private readonly IPropertyRepository _propertyRepo;
    private readonly ISubscriptionLimitChecker _limitChecker;

    public SavePropertyHandler(
        ISavedPropertyRepository savedRepo,
        IPropertyRepository propertyRepo,
        ISubscriptionLimitChecker limitChecker)
    {
        _savedRepo = savedRepo;
        _propertyRepo = propertyRepo;
        _limitChecker = limitChecker;
    }

    public async Task Handle(SavePropertyCommand request, CancellationToken cancellationToken)
    {
        var property = await _propertyRepo.GetByIdAsync(request.PropertyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Property {request.PropertyId} not found.");

        // Already on the wishlist — nothing to do (toggle stays idempotent).
        if (await _savedRepo.ExistsAsync(request.UserId, request.PropertyId, cancellationToken))
            return;

        // Standing-count limit: compare what they have now against the plan max.
        var currentCount = await _savedRepo.CountByUserAsync(request.UserId, cancellationToken);
        var (allowed, reason) = await _limitChecker.CheckSavedListingLimitAsync(
            request.UserId, currentCount, cancellationToken);
        if (!allowed)
            throw new InvalidOperationException(reason);

        await _savedRepo.AddAsync(SavedProperty.Create(request.UserId, property.Id), cancellationToken);
        await _savedRepo.SaveChangesAsync(cancellationToken);
    }
}
