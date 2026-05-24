using MediatR;
using Property.Application.DTOs;

namespace Property.Application.Features.Queries.GetFeaturedProperties;

public record GetFeaturedPropertiesQuery : IRequest<List<PropertySummaryDto>>;
