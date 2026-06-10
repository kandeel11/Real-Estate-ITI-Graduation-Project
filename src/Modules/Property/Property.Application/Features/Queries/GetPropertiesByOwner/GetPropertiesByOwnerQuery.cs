using MediatR;
using Property.Application.DTOs;

namespace Property.Application.Features.Queries.GetPropertiesByOwner;

public record GetPropertiesByOwnerQuery(Guid OwnerId) : IRequest<List<MyPropertySummaryDto>>;
