using System.Threading.Channels;
using Identity.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Email;

public sealed class ChannelEmailJobScheduler : IEmailJobScheduler
{
    private readonly ChannelWriter<EmailJobWorkItem> _writer;
    private readonly ILogger<ChannelEmailJobScheduler> _logger;

    public ChannelEmailJobScheduler(
        Channel<EmailJobWorkItem> channel,
        ILogger<ChannelEmailJobScheduler> logger)
    {
        _writer = channel.Writer;
        _logger = logger;
    }

    public string EnqueueOtp(Guid userId, string otpCode)
    {
        Write(new EmailJobWorkItem(EmailJobType.Otp, userId, OtpCode: otpCode));
        return $"otp:{userId}";
    }

    public string EnqueueVerificationLink(Guid userId)
    {
        Write(new EmailJobWorkItem(EmailJobType.VerificationLink, userId));
        return $"verification:{userId}";
    }

    public string EnqueueWelcome(Guid userId)
    {
        Write(new EmailJobWorkItem(EmailJobType.Welcome, userId));
        return $"welcome:{userId}";
    }

    public string EnqueuePasswordReset(Guid userId, string resetToken)
    {
        Write(new EmailJobWorkItem(EmailJobType.PasswordReset, userId, ResetToken: resetToken));
        return $"reset:{userId}";
    }

    private void Write(EmailJobWorkItem item)
    {
        if (!_writer.TryWrite(item))
            _logger.LogWarning(
                "Email channel is full — dropping {Type} for user {UserId}.",
                item.Type, item.UserId);
    }
}
