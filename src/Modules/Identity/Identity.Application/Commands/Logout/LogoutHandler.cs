using Identity.Application.Interfaces;
using MediatR;

namespace Identity.Application.Commands.Logout;

public class LogoutHandler : IRequestHandler<LogoutCommand>
{
    private readonly IRefreshTokenRepository _refreshTokenRepo;

    public LogoutHandler(IRefreshTokenRepository refreshTokenRepo)
    {
        _refreshTokenRepo = refreshTokenRepo;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var token = await _refreshTokenRepo.GetByTokenAsync(request.RefreshToken, cancellationToken);
        if (token is null || !token.IsActive) return;

        token.Revoke();
        await _refreshTokenRepo.SaveChangesAsync(cancellationToken);
    }
}
