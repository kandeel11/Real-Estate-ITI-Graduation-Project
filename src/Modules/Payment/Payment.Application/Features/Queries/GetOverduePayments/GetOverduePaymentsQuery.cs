using MediatR;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;

namespace Payment.Application.Features.Queries.GetOverduePayments;

public sealed record GetOverduePaymentsQuery : IRequest<IReadOnlyList<RentPaymentDto>>;

