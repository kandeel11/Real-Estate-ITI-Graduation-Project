using Identity.Application.Interfaces;
using MediatR;

namespace Identity.Application.Commands.UpdateProfile;

public record UpdateProfileCommand(
    Guid UserId,
    string FullName,
    string Phone,
    string? Email) : IRequest;

public class UpdateProfileHandler(IUserRepository repo) : IRequestHandler<UpdateProfileCommand>
{
    public async Task Handle(UpdateProfileCommand cmd, CancellationToken ct)
    {
        var user = await repo.GetByIdAsync(cmd.UserId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        user.UpdateProfile(cmd.FullName, cmd.Phone, cmd.Email);
        await repo.UpdateAsync(user, ct);
        await repo.SaveChangesAsync(ct);
    }
}