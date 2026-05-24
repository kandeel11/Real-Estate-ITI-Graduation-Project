using MediatR;
using Property.Application.DTOs;
using Property.Application.Interfaces;

namespace Property.Application.Features.Queries.GetFeaturedProperties;

public class GetFeaturedPropertiesHandler : IRequestHandler<GetFeaturedPropertiesQuery, List<PropertySummaryDto>>
{
    private readonly IPropertyRepository _repository;

    public GetFeaturedPropertiesHandler(IPropertyRepository repository)
    {
        _repository = repository;
    }

    public Task<List<PropertySummaryDto>> Handle(GetFeaturedPropertiesQuery request, CancellationToken cancellationToken)
        => _repository.GetFeaturedAsync(cancellationToken);
}
