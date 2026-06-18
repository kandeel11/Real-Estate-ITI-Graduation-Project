using MediatR;

namespace Payment.Application.Features.Commands.ApplyLateFee;

/// <summary>
/// Command: تطبيق غرامة التأخير على دفعة معينة
/// </summary>
public sealed record ApplyLateFeeCommand(
    Guid PaymentId,
    decimal LateFeeAmount
) : IRequest<bool>;
