using MediatR;
using Property.Application.DTOs;

namespace Property.Application.Features.Queries.GetAllAmenities;

public record GetAllAmenitiesQuery : IRequest<List<AmenityDto>>;
