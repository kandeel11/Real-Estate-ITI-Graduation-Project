namespace AI.Application.DTOs
{

    public sealed record ExtractedLeaseData(
        ExtractedLeaseField RentAmount,
        ExtractedLeaseField SecurityDeposit,
        ExtractedLeaseField StartDate,
        ExtractedLeaseField EndDate,
        ExtractedLeaseField PaymentDayOfMonth,
        ExtractedLeaseField TenantName,
        ExtractedLeaseField OwnerName,
        decimal OverallConfidence,
        string RawAiResponse
    )
    {
        public bool AllFieldsConfident =>
            RentAmount.ConfidenceScore >= 80 &&
            SecurityDeposit.ConfidenceScore >= 80 &&
            StartDate.ConfidenceScore >= 80 &&
            EndDate.ConfidenceScore >= 80 &&
            PaymentDayOfMonth.ConfidenceScore >= 80 &&
            TenantName.ConfidenceScore >= 80 &&
            OwnerName.ConfidenceScore >= 80;

        public string SummaryMessage => AllFieldsConfident
            ? "All fields above 80% — looks good!"
            : "Some fields need your attention — highlighted in yellow.";
    }
}
