namespace AI.Application.DTOs
{
    public sealed record ExtractedLeaseData(
        ExtractedLeaseField InstallmentAmount,
        ExtractedLeaseField PaymentFrequency,
        ExtractedLeaseField TotalRentAmount,
        ExtractedLeaseField DurationInMonths,
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
            InstallmentAmount.ConfidenceScore >= 80 &&
            SecurityDeposit.ConfidenceScore >= 80 &&
            StartDate.ConfidenceScore >= 80 &&
            PaymentFrequency.ConfidenceScore >= 80 &&
            TenantName.ConfidenceScore >= 80 &&
            OwnerName.ConfidenceScore >= 80;

        public string SummaryMessage => AllFieldsConfident
            ? "All fields above 80% — looks good!"
            : "Some fields need your attention — highlighted in yellow.";
    }
}
