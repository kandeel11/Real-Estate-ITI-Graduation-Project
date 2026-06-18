using MediatR;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;

namespace Payment.Application.Features.Queries.GetOverduePayments;

/// <summary>
/// Query: جيب الدفعات المتأخرة (Status=Pending و DueDate في الماضي)
/// </summary>
public sealed record GetOverduePaymentsQuery : IRequest<IReadOnlyList<RentPaymentDto>>;

