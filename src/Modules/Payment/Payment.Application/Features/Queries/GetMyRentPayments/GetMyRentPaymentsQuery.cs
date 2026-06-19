using MediatR;
using Payment.Application.DTOs;

namespace Payment.Application.Features.Queries.GetMyRentPayments;

public sealed record GetMyRentPaymentsQuery(Guid UserId, string Role) : IRequest<IReadOnlyList<RentPaymentDto>>;
