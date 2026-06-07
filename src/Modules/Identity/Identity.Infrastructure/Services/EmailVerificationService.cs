using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Services;

public class EmailVerificationService : IEmailVerificationService
{
    private readonly ILogger<EmailVerificationService> _logger;

    public EmailVerificationService(ILogger<EmailVerificationService> logger)
    {
        _logger = logger;
    }

    public Task SendVerificationEmailAsync(User user, CancellationToken ct)
    {
        // For demo: log the token so testers can copy it from console
        _logger.LogInformation(
            "EMAIL VERIFICATION | To: {Email} | Token: {Token} | Call: GET /api/auth/verify-email?token={Token}",
            user.Email, user.EmailVerificationToken, user.EmailVerificationToken);

        return Task.CompletedTask;
    }

    public Task SendWelcomeEmailAsync(User user, CancellationToken ct)
    {
        _logger.LogInformation(
            "WELCOME EMAIL | To: {Email} | Name: {Name}", user.Email, user.FullName);

        return Task.CompletedTask;
    }
}
