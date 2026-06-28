using MediatR;

namespace Payment.Application.Features.Commands.WaiveLateFee;

/// <summary>
/// Waives the late fee on a rent payment and regenerates the Stripe checkout link
/// at the base amount (no late fee). Authorized callers: landlord (owner), company_manager, admin.
/// SuccessUrl/CancelUrl are passed from the Host layer so the Application layer
/// stays independent of Infrastructure settings.
/// </summary>
public sealed record WaiveLateFeeCommand(
    Guid PaymentId,
    Guid RequesterId,
    string RequesterRole,
    string SuccessUrl,
    string CancelUrl
) : IRequest<bool>;
