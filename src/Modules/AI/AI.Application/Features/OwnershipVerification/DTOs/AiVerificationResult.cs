// AI.Application/Features/OwnershipVerification/DTOs/AiVerificationResult.cs

public class AiVerificationResult
{
    // ── Existing ──
    public decimal NameMatchScore { get; set; }
    public decimal AuthenticityScore { get; set; }
    public decimal ConfidenceScore { get; set; }
    public bool FakeDetectionFlag { get; set; }
    public string Recommendation { get; set; } = string.Empty;
    public string AnalysisNotes { get; set; } = string.Empty;
    public string ExtractedOwnerName { get; set; } = string.Empty;
    public string ExtractedIdExpiry { get; set; } = string.Empty;

    // ── 🆕 Location extraction ──
    public string? ExtractedCountry { get; set; }
    public string? ExtractedGovernorate { get; set; }
    public string? ExtractedCity { get; set; }
    public string? ExtractedDistrict { get; set; }
    public string? ExtractedAddress { get; set; }

    // ── 🆕 Location match ──
    public decimal LocationMatchScore { get; set; }
    public bool LocationMismatchFlag { get; set; }
    public LocationMatchDetails? LocationMatch { get; set; }
}

public sealed class LocationMatchDetails
{
    public decimal CountryScore { get; set; }
    public decimal GovernorateScore { get; set; }
    public decimal CityScore { get; set; }
    public decimal DistrictScore { get; set; }
    public decimal AddressScore { get; set; }
    public string? Notes { get; set; }
}