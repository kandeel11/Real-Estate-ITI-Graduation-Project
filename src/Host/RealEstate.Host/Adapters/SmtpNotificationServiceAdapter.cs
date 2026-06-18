using Identity.Infrastructure.Email;
using Microsoft.Extensions.Logging;
using Payment.Application.Interfaces;

namespace RealEstate.Host.Adapters;

/// <summary>
/// Bridges Identity's SmtpEmailSender into the Payment module's INotificationService contract.
/// Replaces MockNotificationService — registered before AddPaymentModule() so TryAddScoped is a no-op.
/// SMS has no provider configured; calls are logged so they remain traceable.
/// </summary>
public sealed class SmtpNotificationServiceAdapter : INotificationService
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<SmtpNotificationServiceAdapter> _logger;

    public SmtpNotificationServiceAdapter(IEmailSender emailSender, ILogger<SmtpNotificationServiceAdapter> logger)
    {
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task SendEmailAsync(SendEmailRequest request, CancellationToken cancellationToken = default)
    {
        await _emailSender.SendAsync(
            toEmail: request.ToEmail,
            toName: request.ToName,
            subject: request.Subject,
            htmlBody: request.Body,
            ct: cancellationToken);
    }

    public Task SendSmsAsync(SendSmsRequest request, CancellationToken cancellationToken = default)
    {
        // No SMS provider is configured. Log so the message is at least traceable.
        _logger.LogInformation(
            "[Notification] SMS (no provider) → {Phone}: {Message}",
            request.PhoneNumber, request.Message);
        return Task.CompletedTask;
    }
}
