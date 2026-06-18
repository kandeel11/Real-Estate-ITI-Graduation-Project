using MediatR;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Payment.Domain.Enums;

namespace Payment.Application.Features.Queries.GetFinancialSummary;
public sealed record GetFinancialSummaryQuery : IRequest<FinancialSummaryDto>;
