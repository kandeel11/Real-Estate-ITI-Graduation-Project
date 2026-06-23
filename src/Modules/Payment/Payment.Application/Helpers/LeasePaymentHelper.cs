namespace Payment.Application.Helpers;

internal static class LeasePaymentHelper
{
    private static int MonthsPerPeriod(string? frequency) => frequency switch
    {
        "Monthly"      => 1,
        "Bimonthly"    => 2,
        "Quarterly"    => 3,
        "SemiAnnually" => 6,
        "Annually"     => 12,
        _              => 1
    };

    /// <summary>
    /// Returns the next upcoming due date for a lease. If today is on or before the
    /// current period's due date, that date is returned. Otherwise the next period's
    /// due date is returned.
    /// </summary>
    public static DateOnly ComputeNextDueDate(
        DateOnly startDate, string? paymentFrequency, int paymentDayOfMonth, DateOnly today)
    {
        int mpp = MonthsPerPeriod(paymentFrequency);
        int monthsElapsed = MonthsDiff(startDate, today);

        if (monthsElapsed < 0)
            return ClampDay(startDate.Year, startDate.Month, paymentDayOfMonth);

        int periodIndex = monthsElapsed / mpp;
        var periodStart = startDate.AddMonths(periodIndex * mpp);
        var dueDate = ClampDay(periodStart.Year, periodStart.Month, paymentDayOfMonth);

        if (today <= dueDate)
            return dueDate;

        var nextPeriodStart = startDate.AddMonths((periodIndex + 1) * mpp);
        return ClampDay(nextPeriodStart.Year, nextPeriodStart.Month, paymentDayOfMonth);
    }

    /// <summary>
    /// Returns true only when targetMonth is the first month of a payment period.
    /// The background job uses this to skip leases that have no payment due this month.
    /// </summary>
    public static bool IsPeriodStartMonth(DateOnly startDate, string? paymentFrequency, DateOnly targetMonth)
    {
        int mpp = MonthsPerPeriod(paymentFrequency);
        int monthsFromStart = MonthsDiff(startDate, targetMonth);
        return monthsFromStart >= 0 && monthsFromStart % mpp == 0;
    }

    public static DateOnly DueDateForMonth(int year, int month, int paymentDayOfMonth)
        => ClampDay(year, month, paymentDayOfMonth);

    private static int MonthsDiff(DateOnly from, DateOnly to)
        => (to.Year - from.Year) * 12 + (to.Month - from.Month);

    private static DateOnly ClampDay(int year, int month, int day)
        => new DateOnly(year, month, Math.Min(day, DateTime.DaysInMonth(year, month)));
}
