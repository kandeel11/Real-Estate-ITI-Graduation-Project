using MediatR;
using Property.Application.DTOs;
using Property.Application.Interfaces;

namespace Property.Application.Features.Queries.GetPropertyAmenities;

public class GetPropertyAmenitiesHandler : IRequestHandler<GetPropertyAmenitiesQuery, List<AmenityDto>>
{
    private readonly IPropertyRepository _repository;

    public GetPropertyAmenitiesHandler(IPropertyRepository repository)
    {
        _repository = repository;
    }

    public Task<List<AmenityDto>> Handle(GetPropertyAmenitiesQuery request, CancellationToken cancellationToken)
        => _repository.GetPropertyAmenitiesAsync(request.PropertyId, cancellationToken);
}
