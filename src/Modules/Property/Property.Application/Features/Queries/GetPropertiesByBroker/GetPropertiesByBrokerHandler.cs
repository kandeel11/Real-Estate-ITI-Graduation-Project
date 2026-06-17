using MediatR;
using Property.Application.DTOs;
using Property.Application.Interfaces;

namespace Property.Application.Features.Queries.GetPropertiesByBroker;

public class GetPropertiesByBrokerHandler : IRequestHandler<GetPropertiesByBrokerQuery, List<PropertySummaryDto>>
{
    private readonly IPropertyRepository _repository;

    public GetPropertiesByBrokerHandler(IPropertyRepository repository)
        => _repository = repository;

    public Task<List<PropertySummaryDto>> Handle(GetPropertiesByBrokerQuery request, CancellationToken cancellationToken)
        => _repository.GetByBrokerAsync(request.BrokerUserId, cancellationToken);
}
