using Identity.Application.Interfaces;
using MediatR;

namespace Identity.Application.Commands.VerifyEmail;

public class VerifyEmailHandler : IRequestHandler<VerifyEmailCommand, bool>
{
    private readonly IUserRepository _userRepo;
    private readonly IEmailVerificationService _emailService;

    public VerifyEmailHandler(IUserRepository userRepo, IEmailVerificationService emailService)
    {
        _userRepo = userRepo;
        _emailService = emailService;
    }

    public async Task<bool> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepo.GetByVerificationTokenAsync(request.Token, cancellationToken);
        if (user is null) return false;

        try
        {
            user.VerifyEmail(request.Token);
            await _userRepo.SaveChangesAsync(cancellationToken);
            _ = _emailService.SendWelcomeEmailAsync(user, CancellationToken.None);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
