using Identity.Application.Interfaces;
using MediatR;

namespace Identity.Application.Commands.ResendVerificationEmail;

public class ResendVerificationEmailHandler : IRequestHandler<ResendVerificationEmailCommand>
{
    private readonly IUserRepository _userRepo;
    private readonly IEmailVerificationService _emailService;

    public ResendVerificationEmailHandler(IUserRepository userRepo, IEmailVerificationService emailService)
    {
        _userRepo = userRepo;
        _emailService = emailService;
    }

    public async Task Handle(ResendVerificationEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepo.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null || user.IsEmailVerified) return;

        user.RegenerateVerificationToken();
        await _userRepo.SaveChangesAsync(cancellationToken);

        _ = _emailService.SendVerificationEmailAsync(user, CancellationToken.None);
    }
}
