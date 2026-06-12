using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Payment.Domain.Enums;

namespace Payment.Application.Features.Queries.GetPaymentsByStatus;

public sealed record GetPaymentsByStatusQuery(string Status) : IRequest<IReadOnlyList<RentPaymentDto>>;

