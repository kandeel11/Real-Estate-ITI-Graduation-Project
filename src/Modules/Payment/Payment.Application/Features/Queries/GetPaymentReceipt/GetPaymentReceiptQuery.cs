using MediatR;
using Payment.Application.Interfaces;
using Payment.Application.Exceptions;
using Payment.Domain.Enums;

namespace Payment.Application.Features.Queries.GetPaymentReceipt;

// ── DTO ───────────────────────────────────────────────────────
/// <summary>
/// الإيصال الرسمي للدفعة — بيتبعت للمستأجر ويتعرض على الـ UI
/// </summary>
public sealed record PaymentReceiptDto(
    Guid PaymentId,
    Guid LeaseId,
    string TenantName,
    string TenantEmail,
    decimal Amount,
    decimal LateFee,
    decimal TotalPaid,
    DateOnly DueDate,
    DateTime PaidAt,
    string PaymentMethod,
    string TransactionId,
    string? ReceiptUrl,
    string ReceiptNumber  // رقم الإيصال بصيغة بشرية: RCP-2026-000123
);

// ── Query ─────────────────────────────────────────────────────
public sealed record GetPaymentReceiptQuery(Guid PaymentId) : IRequest<PaymentReceiptDto>;

// ── Handler ───────────────────────────────────────────────────
/// <summary>
/// Handler بيجمع بيانات الإيصال من Payment + Lease
/// بيرمي PaymentNotFoundException لو مش موجودة
/// بيرمي InvalidPaymentStateException لو الدفعة لسه مش Paid
/// </summary>
public sealed class GetPaymentReceiptHandler
    : IRequestHandler<GetPaymentReceiptQuery, PaymentReceiptDto>
{
    private readonly IPaymentRepository _repository;
    private readonly ILeaseService _leaseService;

    public GetPaymentReceiptHandler(
        IPaymentRepository repository,
        ILeaseService leaseService)
    {
        _repository = repository;
        _leaseService = leaseService;
    }

    public async Task<PaymentReceiptDto> Handle(
        GetPaymentReceiptQuery request,
        CancellationToken cancellationToken)
    {
        var payment = await _repository.GetByIdAsync(request.PaymentId, cancellationToken)
            ?? throw new PaymentNotFoundException(request.PaymentId);

        if (payment.Status != PaymentStatus.Paid)
            throw new InvalidPaymentStateException(
                request.PaymentId,
                currentStatus: payment.Status.ToString(),
                requiredStatus: PaymentStatus.Paid.ToString());

        // جيب بيانات المستأجر من الـ Lease module
        var lease = await _leaseService.GetLeaseByIdAsync(payment.LeaseId, cancellationToken);
        var tenantName = lease?.TenantName ?? "Unknown Tenant";
        var tenantEmail = lease?.TenantEmail ?? "";

        // توليد رقم إيصال بشري: RCP-2026-000123
        var receiptNumber = $"RCP-{payment.PaidAt!.Value.Year}-{payment.Id.ToString()[..6].ToUpper()}";

        return new PaymentReceiptDto(
            PaymentId: payment.Id,
            LeaseId: payment.LeaseId,
            TenantName: tenantName,
            TenantEmail: tenantEmail,
            Amount: payment.Amount,
            LateFee: payment.LateFee,
            TotalPaid: payment.Amount + payment.LateFee,
            DueDate: payment.DueDate,
            PaidAt: payment.PaidAt!.Value,
            PaymentMethod: payment.PaymentMethod.ToString(),
            TransactionId: payment.TransactionId ?? "",
            ReceiptUrl: payment.ReceiptUrl,
            ReceiptNumber: receiptNumber
        );
    }
}
