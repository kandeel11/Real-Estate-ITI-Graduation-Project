using MediatR;

namespace Payment.Application.Features.Commands.MarkAsPaidExternally;

/// <summary>
/// Admin command: marks a rent payment as settled outside the platform
/// (e.g. cash first-installment).  The payment is excluded from the
/// overdue queue and cannot receive a late fee.
/// </summary>
public sealed record MarkAsPaidExternallyCommand(
    Guid PaymentId,
    string? Notes = null
) : IRequest<bool>;
