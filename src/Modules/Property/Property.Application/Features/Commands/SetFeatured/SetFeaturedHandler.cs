using MediatR;
using Property.Application.Interfaces;

namespace Property.Application.Features.Commands.SetFeatured;

public class SetFeaturedHandler : IRequestHandler<SetFeaturedCommand>
{
    private readonly IPropertyRepository _repository;

    public SetFeaturedHandler(IPropertyRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(SetFeaturedCommand request, CancellationToken cancellationToken)
    {
        var property = await _repository.GetByIdAsync(request.PropertyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Property {request.PropertyId} not found.");

        if (property.OwnerId != request.RequestingUserId)
            throw new UnauthorizedAccessException("Only the property owner can manage featured status.");

        property.SetFeatured(request.IsFeatured, request.FeaturedUntil);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
