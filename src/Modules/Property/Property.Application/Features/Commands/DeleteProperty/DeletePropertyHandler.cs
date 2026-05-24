using MediatR;
using Property.Application.Interfaces;

namespace Property.Application.Features.Commands.DeleteProperty;

public class DeletePropertyHandler : IRequestHandler<DeletePropertyCommand>
{
    private readonly IPropertyRepository _repository;

    public DeletePropertyHandler(IPropertyRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(DeletePropertyCommand request, CancellationToken cancellationToken)
    {
        var property = await _repository.GetByIdAsync(request.PropertyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Property {request.PropertyId} not found.");

        if (property.OwnerId != request.RequestingUserId)
            throw new UnauthorizedAccessException("Only the property owner can delete this listing.");

        property.SoftDelete();
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
