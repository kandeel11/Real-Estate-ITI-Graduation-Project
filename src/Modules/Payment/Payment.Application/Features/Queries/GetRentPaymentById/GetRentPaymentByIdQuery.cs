using MediatR;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;

namespace Payment.Application.Features.Queries.GetRentPaymentById;

public sealed record GetRentPaymentByIdQuery(Guid Id) : IRequest<RentPaymentDto?>;
