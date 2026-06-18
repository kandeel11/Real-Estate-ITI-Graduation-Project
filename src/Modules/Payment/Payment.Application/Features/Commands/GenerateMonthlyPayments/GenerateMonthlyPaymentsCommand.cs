using MediatR;

namespace Payment.Application.Features.Commands.GenerateMonthlyPayments;

/// <summary>
/// Command: توليد دفعات الإيجار الشهرية للـ leases الأكتيف
/// بيتنفذ في بداية كل شهر (يدوي أو عبر background job)
/// </summary>
public sealed record GenerateMonthlyPaymentsCommand(
    /// <summary>
    /// الشهر والسنة اللي هنولّد الدفعات ليه
    /// لو null هيأخذ الشهر الحالي
    /// </summary>
    DateOnly? TargetMonth = null
) : IRequest<GenerateMonthlyPaymentsResult>;

public sealed record GenerateMonthlyPaymentsResult(
    int GeneratedCount,
    int SkippedCount,
    IReadOnlyList<string> Errors
);
