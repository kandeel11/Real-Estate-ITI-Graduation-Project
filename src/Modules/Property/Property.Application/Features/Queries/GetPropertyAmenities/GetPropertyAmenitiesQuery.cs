using MediatR;
using Property.Application.DTOs;

namespace Property.Application.Features.Queries.GetPropertyAmenities;

public record GetPropertyAmenitiesQuery(Guid PropertyId) : IRequest<List<AmenityDto>>;
