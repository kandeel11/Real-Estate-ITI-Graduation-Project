using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using DomainEntities = Identity.Domain.Entities;
using MediatR;

namespace Identity.Application.Commands.RefreshToken;

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly IUserRepository _userRepo;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IJwtService _jwt;

    public RefreshTokenHandler(
        IUserRepository userRepo,
        IRefreshTokenRepository refreshTokenRepo,
        IJwtService jwt)
    {
        _userRepo = userRepo;
        _refreshTokenRepo = refreshTokenRepo;
        _jwt = jwt;
    }

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var existing = await _refreshTokenRepo.GetByTokenAsync(request.Token, cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (!existing.IsActive)
            throw new UnauthorizedAccessException("Refresh token has expired or been revoked.");

        var user = await _userRepo.GetByIdAsync(existing.UserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("User not found.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is deactivated.");

        var newRefreshTokenValue = _jwt.GenerateRefreshToken();
        var newRefreshToken = DomainEntities.RefreshToken.Create(
            userId: user.Id,
            token: newRefreshTokenValue,
            expiresAt: DateTime.UtcNow.AddDays(7),
            createdByIp: request.IpAddress);

        existing.Revoke(replacedBy: newRefreshTokenValue);
        await _refreshTokenRepo.AddAsync(newRefreshToken, cancellationToken);
        await _refreshTokenRepo.SaveChangesAsync(cancellationToken);

        var accessToken = _jwt.GenerateAccessToken(user);

        return new AuthResponseDto(
            AccessToken: accessToken,
            RefreshToken: newRefreshTokenValue,
            ExpiresAt: DateTime.UtcNow.AddMinutes(15),
            User: new CurrentUserDto(
                user.Id, user.Email, user.FullName, user.Role,
                user.CompanyId, user.IsEmailVerified));
    }
}
