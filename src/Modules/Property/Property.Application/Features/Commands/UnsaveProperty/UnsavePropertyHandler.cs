using MediatR;
using Property.Application.Interfaces;

namespace Property.Application.Features.Commands.UnsaveProperty;

public class UnsavePropertyHandler : IRequestHandler<UnsavePropertyCommand>
{
    private readonly ISavedPropertyRepository _savedRepo;

    public UnsavePropertyHandler(ISavedPropertyRepository savedRepo)
    {
        _savedRepo = savedRepo;
    }

    public async Task Handle(UnsavePropertyCommand request, CancellationToken cancellationToken)
    {
        var saved = await _savedRepo.GetAsync(request.UserId, request.PropertyId, cancellationToken);
        if (saved is null) return; // not saved — idempotent no-op

        await _savedRepo.RemoveAsync(saved, cancellationToken);
        await _savedRepo.SaveChangesAsync(cancellationToken);
    }
}
