using MediatR;
using Property.Application.DTOs;
using Property.Application.Interfaces;

namespace Property.Application.Features.Queries.GetPropertyMedia;

public record GetPropertyMediaQuery(Guid PropertyId) : IRequest<List<PropertyMediaDto>>;

public class GetPropertyMediaHandler(IPropertyRepository repository)
    : IRequestHandler<GetPropertyMediaQuery, List<PropertyMediaDto>>
{
    public async Task<List<PropertyMediaDto>> Handle(GetPropertyMediaQuery q, CancellationToken ct)
        => await repository.GetMediaAsync(q.PropertyId, ct);
}