using Identity.Application.Interfaces;
using MediatR;

namespace Identity.Application.Queries.GetUsers;

public class GetUsersHandler : IRequestHandler<GetUsersQuery, PagedUsersDto>
{
    private readonly IUserRepository _users;

    public GetUsersHandler(IUserRepository users) => _users = users;

    public async Task<PagedUsersDto> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var (items, total) = await _users.GetPagedAsync(
            request.Page, pageSize, request.Search, request.Role, cancellationToken);

        return new PagedUsersDto(
            items.Select(u => new AdminUserDto(
                u.Id,
                u.Email,
                u.FullName,
                u.Phone,
                u.Role,
                u.CompanyId,
                u.IsActive,
                u.IsEmailVerified,
                u.BanReason,
                u.BannedAt,
                u.LastLoginAt,
                u.CreatedAt)).ToList(),
            total,
            Math.Max(request.Page, 1),
            pageSize);
    }
}
