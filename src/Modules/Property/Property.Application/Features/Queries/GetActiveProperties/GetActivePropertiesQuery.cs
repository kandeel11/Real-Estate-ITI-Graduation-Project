using MediatR;
using Property.Application.DTOs;

namespace Property.Application.Features.Queries.GetActiveProperties;

public record GetActivePropertiesQuery : IRequest<List<PropertySummaryDto>>;
