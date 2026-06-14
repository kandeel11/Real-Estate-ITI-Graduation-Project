using MediatR;
using Property.Application.DTOs;
using Property.Application.Interfaces;

namespace Property.Application.Features.Queries.GetPropertiesByCompany;

public class GetPropertiesByCompanyHandler : IRequestHandler<GetPropertiesByCompanyQuery, List<PropertySummaryDto>>
{
    private readonly IPropertyRepository _repository;

    public GetPropertiesByCompanyHandler(IPropertyRepository repository)
        => _repository = repository;

    public Task<List<PropertySummaryDto>> Handle(GetPropertiesByCompanyQuery request, CancellationToken cancellationToken)
        => _repository.GetByCompanyAsync(request.CompanyId, cancellationToken);
}
