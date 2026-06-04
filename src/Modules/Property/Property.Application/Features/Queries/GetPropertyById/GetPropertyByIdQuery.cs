using MediatR;
using Property.Application.DTOs;

namespace Property.Application.Features.Queries.GetPropertyById;

public record GetPropertyByIdQuery(Guid PropertyId) : IRequest<PropertyDetailDto?>;
