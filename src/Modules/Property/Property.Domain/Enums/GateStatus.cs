namespace Property.Domain.Enums;

/// <summary>
/// Outcome of one verification gate (ownership or duplicate). A property only becomes
/// Active when BOTH gates are clear (Passed or NotRequired).
/// </summary>
public enum GateStatus
{
    Pending = 0,     // not evaluated yet
    NotRequired,     // gate doesn't apply (e.g. ownership for a broker)
    Passed,          // cleared automatically
    NeedsReview,     // inconclusive → a human admin must decide
    Failed           // hard fail → property rejected
}
