using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using DomainEntities = Identity.Domain.Entities;
using MediatR;

namespace Identity.Application.Commands.Login;

public class LoginHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly IUserRepository _userRepo;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtService _jwt;

    public LoginHandler(
        IUserRepository userRepo,
        IRefreshTokenRepository refreshTokenRepo,
        IPasswordHasher hasher,
        IJwtService jwt)
    {
        _userRepo = userRepo;
        _refreshTokenRepo = refreshTokenRepo;
        _hasher = hasher;
        _jwt = jwt;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepo.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException(user.BanReason ?? "Account is deactivated.");

        if (!_hasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        var accessToken = _jwt.GenerateAccessToken(user);
        var refreshTokenValue = _jwt.GenerateRefreshToken();

        var refreshToken = DomainEntities.RefreshToken.Create(
            userId: user.Id,
            token: refreshTokenValue,
            expiresAt: DateTime.UtcNow.AddDays(7),
            createdByIp: request.IpAddress);

        await _refreshTokenRepo.AddAsync(refreshToken, cancellationToken);

        user.RecordLogin();
        await _userRepo.SaveChangesAsync(cancellationToken);
        await _refreshTokenRepo.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto(
            AccessToken: accessToken,
            RefreshToken: refreshTokenValue,
            ExpiresAt: DateTime.UtcNow.AddMinutes(15),
            User: new CurrentUserDto(
                user.Id, user.Email, user.FullName, user.Role,
                user.CompanyId, user.IsEmailVerified));
    }
}
