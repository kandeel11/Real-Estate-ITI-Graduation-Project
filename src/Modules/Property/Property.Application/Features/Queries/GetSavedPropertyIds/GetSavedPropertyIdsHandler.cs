using MediatR;
using Property.Application.Interfaces;

namespace Property.Application.Features.Queries.GetSavedPropertyIds;

public class GetSavedPropertyIdsHandler : IRequestHandler<GetSavedPropertyIdsQuery, List<Guid>>
{
    private readonly ISavedPropertyRepository _savedRepo;

    public GetSavedPropertyIdsHandler(ISavedPropertyRepository savedRepo)
    {
        _savedRepo = savedRepo;
    }

    public Task<List<Guid>> Handle(GetSavedPropertyIdsQuery request, CancellationToken cancellationToken)
        => _savedRepo.GetSavedIdsAsync(request.UserId, cancellationToken);
}
