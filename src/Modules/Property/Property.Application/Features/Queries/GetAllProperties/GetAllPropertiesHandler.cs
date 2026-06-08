using MediatR;
using Property.Application.DTOs;
using Property.Application.Interfaces;

namespace Property.Application.Features.Queries.GetAllProperties;

public class GetAllPropertiesHandler : IRequestHandler<GetAllPropertiesQuery, List<PropertySummaryDto>>
{
    private readonly IPropertyRepository _repository;

    public GetAllPropertiesHandler(IPropertyRepository repository)
    {
        _repository = repository;
    }

    public Task<List<PropertySummaryDto>> Handle(GetAllPropertiesQuery request, CancellationToken cancellationToken)
        => _repository.GetAllAsync(cancellationToken);
}
