using MediatR;
using Property.Application.DTOs;

namespace Property.Application.Features.Queries.GetSavedProperties;

public record GetSavedPropertiesQuery(Guid UserId) : IRequest<List<SavedPropertyDto>>;
