namespace Payment.Application.DTOs;
public sealed record ActiveLeaseDto(
    Guid LeaseId,
    decimal InstallmentAmount,
    int PaymentDayOfMonth,
    string TenantName,
    string TenantEmail,
    string? TenantPhone,
    Guid PropertyId = default,
    Guid TenantId = default,
    Guid LandlordId = default
);
