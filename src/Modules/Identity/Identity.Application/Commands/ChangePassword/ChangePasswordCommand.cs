using Identity.Application.Interfaces;
using MediatR;

namespace Identity.Application.Commands.ChangePassword;

public record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword) : IRequest;

public class ChangePasswordHandler(IUserRepository repo, IPasswordHasher hasher)
    : IRequestHandler<ChangePasswordCommand>
{
    public async Task Handle(ChangePasswordCommand cmd, CancellationToken ct)
    {
        var user = await repo.GetByIdAsync(cmd.UserId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        if (!hasher.Verify(cmd.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect.");

        var prop = user.GetType().GetProperty("PasswordHash");
        prop?.SetValue(user, hasher.Hash(cmd.NewPassword));

        user.MarkAsUpdated();
        await repo.UpdateAsync(user, ct);
        await repo.SaveChangesAsync(ct);
    }
}