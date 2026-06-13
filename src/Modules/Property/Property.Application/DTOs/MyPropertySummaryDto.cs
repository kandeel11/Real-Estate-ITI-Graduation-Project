namespace Property.Application.DTOs;

/// <summary>
/// Owner/admin view of a property summary — extends the public summary with verification
/// state (status, gates, duplicate score, rejection note). Returned only by authenticated
/// owner/admin endpoints, never by public listing endpoints.
/// </summary>
public class MyPropertySummaryDto : PropertySummaryDto
{
    public string VerificationStatus { get; init; } = string.Empty;
    public string? RejectionReason { get; init; }
    public decimal? AiDuplicateConfidence { get; init; }
    public string OwnershipGate { get; init; } = string.Empty;
    public string DuplicateGate { get; init; } = string.Empty;
}
