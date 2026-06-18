namespace Payment.Application.Interfaces;

/// <summary>
/// Contract للتواصل مع الـ Notification Module
/// Payment module بيستخدمه لإرسال إيميلات وSMS عند:
/// - نجاح الدفع
/// - فشل الدفع
/// - تطبيق غرامة التأخير
/// - تذكير بالدفعات المتأخرة
/// </summary>
public interface INotificationService
{
    Task SendEmailAsync(SendEmailRequest request, CancellationToken cancellationToken = default);
    Task SendSmsAsync(SendSmsRequest request, CancellationToken cancellationToken = default);
}

public sealed record SendEmailRequest(
    string ToEmail,
    string ToName,
    string Subject,
    string Body,
    bool IsHtml = true
);

public sealed record SendSmsRequest(
    string PhoneNumber,
    string Message
);
