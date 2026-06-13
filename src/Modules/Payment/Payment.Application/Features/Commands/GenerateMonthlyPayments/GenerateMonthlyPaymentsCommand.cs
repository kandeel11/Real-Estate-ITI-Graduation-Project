using MediatR;

namespace Payment.Application.Features.Commands.GenerateMonthlyPayments;

public sealed record GenerateMonthlyPaymentsCommand(
    DateOnly? TargetMonth = null
) : IRequest<GenerateMonthlyPaymentsResult>;

public sealed record GenerateMonthlyPaymentsResult(
    int GeneratedCount,
    int SkippedCount,
    IReadOnlyList<string> Errors
);
