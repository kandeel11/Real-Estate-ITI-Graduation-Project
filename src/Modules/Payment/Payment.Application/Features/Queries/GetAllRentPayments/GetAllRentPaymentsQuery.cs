using MediatR;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Payment.Domain.Enums;

namespace Payment.Application.Features.Queries.GetAllRentPayments;
public sealed record GetAllRentPaymentsQuery(
    Guid? LeaseId = null
) : IRequest<IReadOnlyList<RentPaymentDto>>;
