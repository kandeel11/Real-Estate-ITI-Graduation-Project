using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using MediatR;

namespace Identity.Application.Queries.GetCurrentUser;

public class GetCurrentUserHandler : IRequestHandler<GetCurrentUserQuery, UserDto?>
{
    private readonly IUserRepository _userRepo;
    private readonly ICurrentUserService _currentUser;

    public GetCurrentUserHandler(IUserRepository userRepo, ICurrentUserService currentUser)
    {
        _userRepo = userRepo;
        _currentUser = currentUser;
    }

    public async Task<UserDto?> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null) return null;

        var user = await _userRepo.GetByIdAsync(_currentUser.UserId.Value, cancellationToken);

        if (user is null) throw new UnauthorizedAccessException("User no longer exists.");

        return new UserDto(
            user.Id, user.Email, user.FullName, user.Phone,
            user.Role, user.CompanyId, user.IsActive, user.IsEmailVerified);
    }
}