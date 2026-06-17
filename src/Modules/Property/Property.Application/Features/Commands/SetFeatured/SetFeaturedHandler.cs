using MediatR;
using Property.Application.Interfaces;
using RealEstate.Shared.Kernel.Shared.Contracts;

namespace Property.Application.Features.Commands.SetFeatured;

public class SetFeaturedHandler : IRequestHandler<SetFeaturedCommand>
{
    private readonly IPropertyRepository _repository;
    private readonly ISubscriptionLimitChecker _limitChecker;

    public SetFeaturedHandler(IPropertyRepository repository, ISubscriptionLimitChecker limitChecker)
    {
        _repository = repository;
        _limitChecker = limitChecker;
    }

    public async Task Handle(SetFeaturedCommand request, CancellationToken cancellationToken)
    {
        var property = await _repository.GetByIdAsync(request.PropertyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Property {request.PropertyId} not found.");

        // Ownership check: company properties are owned by the company; individual
        // properties are owned by the requesting user.
        bool owns = request.CompanyId.HasValue
            ? property.CompanyId == request.CompanyId
            : property.OwnerId == request.RequestingUserId;

        if (!owns)
            throw new UnauthorizedAccessException("You do not have permission to manage this listing.");

        if (request.IsFeatured)
        {
            // Plan gate — only premium plans include featured listings
            var (allowed, reason) = await _limitChecker.CheckFeaturedListingsAccessAsync(
                request.RequestingUserId, request.CompanyId, cancellationToken);
            if (!allowed)
                throw new InvalidOperationException(reason);

            // 1-at-a-time rule: unfeature the currently featured listing (if any) before
            // featuring the new one so the slot is always occupied by exactly one listing.
            var current = await _repository.GetCurrentlyFeaturedByOwnerAsync(
                request.RequestingUserId, request.CompanyId, cancellationToken);

            if (current is not null && current.Id != request.PropertyId)
                current.SetFeatured(false, null);
        }

        property.SetFeatured(request.IsFeatured, request.FeaturedUntil);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
