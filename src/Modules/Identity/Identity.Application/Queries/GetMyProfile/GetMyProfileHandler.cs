using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using MediatR;

namespace Identity.Application.Queries.GetMyProfile;

public class GetMyProfileHandler : IRequestHandler<GetMyProfileQuery, UserDto?>
{
    private readonly IUserRepository _users;

    public GetMyProfileHandler(IUserRepository users) => _users = users;

    public async Task<UserDto?> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null) return null;

        return new UserDto(
            user.Id,
            user.Email,
            user.FullName,
            user.Phone,
            user.Role,
            user.CompanyId,
            user.IsActive,
            user.IsEmailVerified);
    }
}
