namespace Payment.Application.Exceptions;

/// <summary>
/// Base exception للـ Payment module
/// </summary>
public abstract class PaymentException : Exception
{
    protected PaymentException(string message) : base(message) { }
}

/// <summary>
/// لما الدفعة مش موجودة في الداتابيز
/// بيتحول لـ 404 Not Found في الـ middleware
/// </summary>
public sealed class PaymentNotFoundException : PaymentException
{
    public Guid PaymentId { get; }

    public PaymentNotFoundException(Guid paymentId)
        : base($"Payment with ID '{paymentId}' was not found.")
    {
        PaymentId = paymentId;
    }
}

/// <summary>
/// لما بنحاول نعمل عملية على دفعة في حالة غلط
/// مثلاً: refund على دفعة لسه Pending
/// بيتحول لـ 422 Unprocessable Entity في الـ middleware
/// </summary>
public sealed class InvalidPaymentStateException : PaymentException
{
    public Guid PaymentId { get; }
    public string CurrentStatus { get; }
    public string RequiredStatus { get; }

    public InvalidPaymentStateException(Guid paymentId, string currentStatus, string requiredStatus)
        : base($"Payment '{paymentId}' is in '{currentStatus}' state. Required state: '{requiredStatus}'.")
    {
        PaymentId = paymentId;
        CurrentStatus = currentStatus;
        RequiredStatus = requiredStatus;
    }
}

/// <summary>
/// لما الدفعة اتعملت بالفعل (duplicate)
/// بيتحول لـ 409 Conflict في الـ middleware
/// </summary>
public sealed class DuplicatePaymentException : PaymentException
{
    public Guid LeaseId { get; }
    public string Month { get; }

    public DuplicatePaymentException(Guid leaseId, string month)
        : base($"Payment for Lease '{leaseId}' in '{month}' already exists.")
    {
        LeaseId = leaseId;
        Month = month;
    }
}

/// <summary>
/// لما الـ Stripe Gateway برجع error
/// بيتحول لـ 402 Payment Required في الـ middleware
/// </summary>
public sealed class PaymentGatewayException : PaymentException
{
    public string? GatewayCode { get; }

    public PaymentGatewayException(string message, string? gatewayCode = null)
        : base(message)
    {
        GatewayCode = gatewayCode;
    }
}

/// <summary>
/// لما الخطة المطلوبة غير موجودة أو غير مفعّلة في Subscription module
/// بيتحول لـ 404 Not Found في الـ middleware
/// </summary>
public sealed class PlanNotFoundException : PaymentException
{
    public Guid PlanId { get; }

    public PlanNotFoundException(Guid planId)
        : base($"Subscription plan '{planId}' was not found or is inactive.")
    {
        PlanId = planId;
    }
}
