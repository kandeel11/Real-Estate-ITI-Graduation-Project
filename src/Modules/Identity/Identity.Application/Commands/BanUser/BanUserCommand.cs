using Identity.Application.Interfaces;
using MediatR;

namespace Identity.Application.Commands.BanUser;

public record BanUserCommand(Guid UserId, string Reason) : IRequest;

public class BanUserHandler : IRequestHandler<BanUserCommand>
{
    private readonly IUserRepository _users;
    public BanUserHandler(IUserRepository users) => _users = users;

    public async Task Handle(BanUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        if (user.Role == "admin")
            throw new InvalidOperationException("Admin accounts cannot be banned.");

        user.Ban(string.IsNullOrWhiteSpace(request.Reason) ? "Banned by admin" : request.Reason);
        await _users.SaveChangesAsync(cancellationToken);
    }
}

public record UnbanUserCommand(Guid UserId) : IRequest;

public class UnbanUserHandler : IRequestHandler<UnbanUserCommand>
{
    private readonly IUserRepository _users;
    public UnbanUserHandler(IUserRepository users) => _users = users;

    public async Task Handle(UnbanUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        user.Unban();
        await _users.SaveChangesAsync(cancellationToken);
    }
}
