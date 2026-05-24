using MediatR;
using Property.Application.DTOs;
using Property.Application.Interfaces;

namespace Property.Application.Features.Queries.GetAllAmenities;

public class GetAllAmenitiesHandler : IRequestHandler<GetAllAmenitiesQuery, List<AmenityDto>>
{
    private readonly IPropertyRepository _repository;

    public GetAllAmenitiesHandler(IPropertyRepository repository)
    {
        _repository = repository;
    }

    public Task<List<AmenityDto>> Handle(GetAllAmenitiesQuery request, CancellationToken cancellationToken)
        => _repository.GetAllAmenitiesAsync(cancellationToken);
}
