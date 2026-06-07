using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Infrastructure.Email;
using Microsoft.Extensions.Options;

public sealed class EmailVerificationService : IEmailVerificationService
{
    private readonly IEmailSender _sender;
    private readonly IEmailTemplateRenderer _renderer;
    private readonly EmailSettings _settings;

    public EmailVerificationService(
        IEmailSender sender,
        IEmailTemplateRenderer renderer,
        IOptions<EmailSettings> settings)
    {
        _sender = sender;
        _renderer = renderer;
        _settings = settings.Value;
    }

    public async Task SendOtpAsync(User user, string otpCode, CancellationToken ct)
    {
        var html = await _renderer.RenderAsync("otp", new
        {
            name = user.FullName,
            code = otpCode,
            expires_in_minutes = 10
        });

        await _sender.SendAsync(user.Email, user.FullName,
            $"Your verification code: {otpCode}", html, ct);
    }

    public async Task SendVerificationLinkAsync(User user, CancellationToken ct)
    {
        var link = $"{_settings.AppBaseUrl}/api/auth/verify-email" +
                   $"?token={user.EmailVerificationToken}";

        var html = await _renderer.RenderAsync("verification-link", new
        {
            name = user.FullName,
            link = link
        });

        await _sender.SendAsync(user.Email, user.FullName,
            "Verify your email", html, ct);
    }

    public async Task SendWelcomeEmailAsync(User user, CancellationToken ct)
    {
        var html = await _renderer.RenderAsync("welcome", new { name = user.FullName });
        await _sender.SendAsync(user.Email, user.FullName,
            "Welcome to Real Estate Platform 🎉", html, ct);
    }

    public async Task SendPasswordResetAsync(User user, string resetToken, CancellationToken ct)
    {
        var link = $"{_settings.AppBaseUrl}/api/auth/reset-password?token={resetToken}";
        var html = await _renderer.RenderAsync("password-reset",
            new { name = user.FullName, link });

        await _sender.SendAsync(user.Email, user.FullName,
            "Reset your password", html, ct);
    }
}