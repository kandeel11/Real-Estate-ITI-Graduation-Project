using MediatR;
using Property.Application.DTOs;
using Property.Application.Interfaces;

namespace Property.Application.Features.Queries.GetPropertiesByOwner;

public class GetPropertiesByOwnerHandler : IRequestHandler<GetPropertiesByOwnerQuery, List<MyPropertySummaryDto>>
{
    private readonly IPropertyRepository _repository;

    public GetPropertiesByOwnerHandler(IPropertyRepository repository)
    {
        _repository = repository;
    }

    public Task<List<MyPropertySummaryDto>> Handle(GetPropertiesByOwnerQuery request, CancellationToken cancellationToken)
        => _repository.GetByOwnerAsync(request.OwnerId, cancellationToken);
}
