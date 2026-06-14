using Microsoft.Extensions.Logging;
using Payment.Application.Interfaces;

namespace Payment.Infrastructure.Services;

public sealed class MockNotificationService : INotificationService
{
    private readonly ILogger<MockNotificationService> _logger;

    public MockNotificationService(ILogger<MockNotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(SendEmailRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[MockNotification] Email → To: {To} | Subject: {Subject}",
            request.ToEmail, request.Subject);

        return Task.CompletedTask;
    }

    public Task SendSmsAsync(SendSmsRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[MockNotification] SMS → Phone: {Phone} | Message: {Message}",
            request.PhoneNumber, request.Message);

        return Task.CompletedTask;
    }
}
