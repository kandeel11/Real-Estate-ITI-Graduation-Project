using MediatR;
using Property.Application.DTOs;

namespace Property.Application.Features.Queries.GetAllProperties;

public record GetAllPropertiesQuery : IRequest<List<PropertySummaryDto>>;
