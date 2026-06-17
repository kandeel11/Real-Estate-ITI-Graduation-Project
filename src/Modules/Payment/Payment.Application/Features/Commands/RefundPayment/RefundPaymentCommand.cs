using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Payment.Application.Interfaces;
using Payment.Domain.Enums;

namespace Payment.Application.Features.Commands.RefundPayment;
public sealed record RefundPaymentCommand(
    Guid PaymentId,
    string Reason
) : IRequest<bool>;
