using Identity.Application.Interfaces;
using MediatR;

namespace Identity.Application.Queries.GetUserNames;

/// <summary>
/// Resolves a set of user ids to their full names — used by the Host to
/// enrich cross-module data (e.g. inquiry tenants/brokers) without other
/// modules depending on Identity.
/// </summary>
public record GetUserNamesQuery(IReadOnlyCollection<Guid> UserIds)
    : IRequest<Dictionary<Guid, string>>;

public class GetUserNamesHandler : IRequestHandler<GetUserNamesQuery, Dictionary<Guid, string>>
{
    private readonly IUserRepository _users;
    public GetUserNamesHandler(IUserRepository users) => _users = users;

    public async Task<Dictionary<Guid, string>> Handle(GetUserNamesQuery request, CancellationToken cancellationToken)
    {
        if (request.UserIds.Count == 0)
            return new Dictionary<Guid, string>();

        return await _users.GetFullNamesByIdsAsync(request.UserIds, cancellationToken);
    }
}
