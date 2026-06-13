using MediatR;
using Property.Application.DTOs;
using Property.Application.Interfaces;
using Property.Domain.Enums;

namespace Property.Application.Features.Queries.GetActiveProperties;

public class GetActivePropertiesHandler : IRequestHandler<GetActivePropertiesQuery, List<PropertySummaryDto>>
{
    private readonly IPropertyRepository _repository;

    public GetActivePropertiesHandler(IPropertyRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<PropertySummaryDto>> Handle(GetActivePropertiesQuery request, CancellationToken cancellationToken)
    {
        var allProperties = await _repository.GetAllAsync(cancellationToken);

        // Filter to only Active properties
        var activeProperties = allProperties
            .Where(p => p.Status == PropertyStatus.Active.ToString())
            .ToList();

        return activeProperties;
    }
}
