using MediatR;

namespace Payment.Application.Features.Commands.ProcessPayment;

/// <summary>
/// Command: معالجة دفعة إيجار موجودة عبر الـ Payment Gateway
/// 
/// بيشتغل إزاي:
/// 1. يجيب الدفعة من الداتابيز
/// 2. يبعت للـ gateway
/// 3. لو نجح: Status=Paid، PaidAt=Now، TransactionId مسجل
/// 4. لو فشل: Status=Failed
/// </summary>
public sealed record ProcessPaymentCommand(
    Guid PaymentId,
    string PaymentMethod,
    string? CardToken = null
) : IRequest<ProcessPaymentResult>;

public sealed record ProcessPaymentResult(
    bool IsSuccess,
    string? TransactionId,
    string? ReceiptUrl,
    string? ErrorMessage
);
