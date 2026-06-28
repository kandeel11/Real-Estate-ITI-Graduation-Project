namespace Payment.Application.DTOs;
public sealed record RentPaymentDto(
    Guid Id,
    Guid LeaseId,
    Guid PropertyId,
    Guid TenantId,
    Guid LandlordId,
    decimal Amount,
    DateOnly DueDate,
    DateTime? PaidAt,
    string Status,
    string? PaymentMethod,
    string? TransactionId,
    decimal LateFee,
    bool AiPredictedLate,
    string? ReceiptUrl,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string? CheckoutUrl = null,
    string? CheckoutSessionId = null
);
