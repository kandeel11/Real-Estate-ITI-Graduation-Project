using MediatR;
using Property.Application.DTOs;
using Property.Application.Interfaces;

namespace Property.Application.Features.Queries.GetPendingVerification;

public class GetPendingVerificationHandler : IRequestHandler<GetPendingVerificationQuery, List<MyPropertySummaryDto>>
{
    private readonly IPropertyRepository _repository;

    public GetPendingVerificationHandler(IPropertyRepository repository)
    {
        _repository = repository;
    }

    public Task<List<MyPropertySummaryDto>> Handle(GetPendingVerificationQuery request, CancellationToken cancellationToken)
        => _repository.GetPendingVerificationAsync(cancellationToken);
}
