using MediatR;
using Property.Application.DTOs;
using Property.Application.Interfaces;

namespace Property.Application.Features.Queries.GetSavedProperties;

public class GetSavedPropertiesHandler : IRequestHandler<GetSavedPropertiesQuery, List<SavedPropertyDto>>
{
    private readonly ISavedPropertyRepository _savedRepo;

    public GetSavedPropertiesHandler(ISavedPropertyRepository savedRepo)
    {
        _savedRepo = savedRepo;
    }

    public Task<List<SavedPropertyDto>> Handle(GetSavedPropertiesQuery request, CancellationToken cancellationToken)
        => _savedRepo.GetSavedByUserAsync(request.UserId, cancellationToken);
}
