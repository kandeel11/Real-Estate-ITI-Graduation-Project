using MediatR;

namespace Property.Application.Features.Queries.GetSavedPropertyIds;

public record GetSavedPropertyIdsQuery(Guid UserId) : IRequest<List<Guid>>;
