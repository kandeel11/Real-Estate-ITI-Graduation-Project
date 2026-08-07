using AI.Application.Features.OwnershipVerification.DTOs;
using AI.Application.Features.OwnershipVerification.Interfaces;
using AI.Application.Common.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AI.Infrastructure.Services
{
    /// <summary>
    /// Implementation of IOwnershipAiService using Gemini vision + Groq text analysis.
    ///
    /// Pipeline:
    /// 1. Gemini vision → extract Name, ID#, Expiry from the national ID (structured)
    /// 2. Gemini vision → extract text from the ownership proof/contract
    /// 3. Groq (via Semantic Kernel) → compare names, verify authenticity, return scores
    ///
    /// Replaces Azure Document Intelligence's "prebuilt-idDocument"/"prebuilt-layout"
    /// after the Azure subscription was disabled — steps 1 and 2 now rely on Gemini's
    /// general vision understanding rather than a purpose-built ID/layout model, so
    /// extraction accuracy on real IDs and contracts should be spot-checked.
    /// </summary>
    public class GeminiOwnershipAiService : IOwnershipAiService
    {
        private const string IdExtractionPrompt = """
            Extract the following fields from this national ID document image.
            Return ONLY valid JSON, no markdown, in this exact shape:
            {"firstName": "...", "lastName": "...", "documentNumber": "...", "expirationDate": "yyyy-MM-dd"}
            Use an empty string for any field you cannot find. Format expirationDate
            as yyyy-MM-dd if present, otherwise an empty string.
            """;

        private const string OwnershipTextPrompt = """
            Extract the text content of this ownership proof / contract document,
            focusing on the first 3 pages if it is a multi-page document. Return
            ONLY valid JSON, no markdown, in this exact shape: {"text": "..."}
            """;

        private readonly GeminiVisionClient _vision;
        private readonly Kernel _kernel;
        private readonly ILogger<GeminiOwnershipAiService> _logger;
        private readonly VerificationOptions _verificationOptions;

        public GeminiOwnershipAiService(
            IHttpClientFactory httpClientFactory,
            IOptions<AiOptions> aiOptions,
            Kernel kernel,
            ILogger<GeminiOwnershipAiService> logger,
            IOptions<VerificationOptions> verificationOptions
        )
        {
            var o = aiOptions.Value;
            _vision = new GeminiVisionClient(
                httpClientFactory.CreateClient(nameof(GeminiOwnershipAiService)),
                o.GeminiApiKey,
                o.GeminiVisionModel);
            _kernel = kernel;
            _logger = logger;
            _verificationOptions = verificationOptions.Value;
        }

        /// <summary>
        /// Main entry point: analyze National ID and ownership proof documents.
        /// </summary>
        public async Task<AiVerificationResult> AnalyzeAsync(
     byte[] nationalIdFile,
     string nationalIdMimeType,
     byte[] ownershipProofFile,
     string ownershipProofMimeType,
     string ownerName,
     PropertyLocationDto expectedLocation,
     CancellationToken ct = default)
        {
            _logger.LogInformation("Starting ownership verification analysis");

            try
            {
                var idData = await ExtractNationalIdDataAsync(nationalIdFile, nationalIdMimeType, ct);
                var ownershipText = await ExtractOwnershipProofTextAsync(ownershipProofFile, ownershipProofMimeType, ct);

                var aiResult = await PerformClaudeAnalysisAsync(
                    ownerName, idData, ownershipText, expectedLocation, ct);

                // 🆕 location comparison happens in code (not Claude) for determinism
                aiResult.LocationMatch = ScoreLocation(aiResult, expectedLocation);
                aiResult.LocationMatchScore = aiResult.LocationMatch.AverageScore();
                aiResult.LocationMismatchFlag = aiResult.LocationMatchScore < _verificationOptions.LocationMatchThreshold;

                // 🆕 fold location into overall confidence + recommendation
                AdjustConfidenceForLocation(aiResult);

                return aiResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during ownership verification analysis");
                throw;
            }
        }

        /// <summary>
        /// Extract structured data from National ID using Gemini vision.
        /// Returns: Name, ID Number, Expiry Date
        /// </summary>
        private async Task<Dictionary<string, string>> ExtractNationalIdDataAsync(
            byte[] fileContent,
            string mimeType,
            CancellationToken ct
        )
        {
            var result = new Dictionary<string, string>();

            try
            {
                var json = await _vision.GenerateJsonAsync(IdExtractionPrompt, fileContent, mimeType, ct);
                var parsed = JsonSerializer.Deserialize<IdExtractionResult>(json);

                if (parsed is null)
                {
                    _logger.LogWarning("No ID document detected");
                    return result;
                }

                if (!string.IsNullOrWhiteSpace(parsed.FirstName)) result["FirstName"] = parsed.FirstName;
                if (!string.IsNullOrWhiteSpace(parsed.LastName)) result["LastName"] = parsed.LastName;
                if (!string.IsNullOrWhiteSpace(parsed.DocumentNumber)) result["DocumentNumber"] = parsed.DocumentNumber;
                if (!string.IsNullOrWhiteSpace(parsed.ExpirationDate)) result["ExpirationDate"] = parsed.ExpirationDate;

                _logger.LogInformation("Extracted ID data: {Fields}", string.Join(", ", result.Keys));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting National ID data");
                throw;
            }

            return result;
        }

        /// <summary>
        /// Extract clean text from ownership proof (contract/deed) using Gemini vision.
        /// </summary>
        private async Task<string> ExtractOwnershipProofTextAsync(
            byte[] fileContent,
            string mimeType,
            CancellationToken ct
        )
        {
            try
            {
                var json = await _vision.GenerateJsonAsync(OwnershipTextPrompt, fileContent, mimeType, ct);
                var parsed = JsonSerializer.Deserialize<OwnershipTextResult>(json);

                var text = (parsed?.Text ?? string.Empty).Trim();
                _logger.LogInformation("Extracted {Length} chars from ownership proof", text.Length);

                return text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting ownership proof text");
                throw;
            }
        }

        private sealed class IdExtractionResult
        {
            [JsonPropertyName("firstName")] public string FirstName { get; set; } = string.Empty;
            [JsonPropertyName("lastName")] public string LastName { get; set; } = string.Empty;
            [JsonPropertyName("documentNumber")] public string DocumentNumber { get; set; } = string.Empty;
            [JsonPropertyName("expirationDate")] public string ExpirationDate { get; set; } = string.Empty;
        }

        private sealed class OwnershipTextResult
        {
            [JsonPropertyName("text")] public string Text { get; set; } = string.Empty;
        }

        /// <summary>
        /// Use Groq (via Semantic Kernel) to perform semantic analysis and comparison.
        /// Returns confidence scores and fake detection flags.
        /// </summary>
        private async Task<AiVerificationResult> PerformClaudeAnalysisAsync(
            string submittedOwnerName,
            Dictionary<string, string> idData,
            string ownershipProofText,
            PropertyLocationDto expectedLocation,
            CancellationToken ct
        )
        {
            var extractedOwnerName = GetFullName(idData);
            var extractedIdExpiry = idData.TryGetValue("ExpirationDate", out var expiry) ? expiry : null;

            // Build prompt for Groq
            var prompt = BuildAnalysisPrompt(submittedOwnerName, extractedOwnerName, extractedIdExpiry, ownershipProofText, expectedLocation);

            _logger.LogInformation("Sending analysis prompt to Groq");

            var function = _kernel.CreateFunctionFromPrompt(prompt);
            var result = await _kernel.InvokeAsync(function, cancellationToken: ct);

            var responseText = result.ToString();
            _logger.LogInformation("Groq response: {Response}", responseText);

            var analysisResult = ParseClaudeResponse(responseText, extractedOwnerName, extractedIdExpiry);

            return analysisResult;
        }

        /// <summary>
        /// Build a structured prompt for Groq to analyze ownership documents.
        /// </summary>
        private string BuildAnalysisPrompt(
     string submittedOwnerName,
     string extractedIdName,
     string? idExpiry,
     string ownershipProofText,
     PropertyLocationDto expectedLocation)
        {
            return $@"
You are an expert document analyst specializing in real estate ownership verification.

SUBMITTED OWNER NAME: {submittedOwnerName}
EXTRACTED ID NAME:    {extractedIdName}
ID EXPIRY DATE:       {idExpiry ?? "Not found"}

EXPECTED PROPERTY LOCATION (from listing):
- Country:     {expectedLocation.Country}
- Governorate: {expectedLocation.Governorate}
- City:        {expectedLocation.City}
- District:    {expectedLocation.District ?? "N/A"}
- Address:     {expectedLocation.AddressLine}

OWNERSHIP PROOF TEXT (first 3 pages):
---
{ownershipProofText}
---

Tasks:
1) Verify the owner name matches between the ID and the submitted name.
2) Detect signs of tampering or inconsistencies in the documents.
3) Extract the property location mentioned in the contract/deed:
   - country, governorate, city, district, full address
   - Search both Arabic and English sections. Egyptian contracts often use: 'محافظة' (governorate), 'مدينة' (city), 'حي' (district), 'شارع' (street), 'عنوان' (address).
   - IMPORTANT: Always return location values in ENGLISH (translate or transliterate from Arabic).
     Examples: 'الجيزة' → 'Giza', 'القاهرة' → 'Cairo', 'الإسكندرية' → 'Alexandria', 'مصر' → 'Egypt'.
   - Do NOT just copy the expected location; only extract what the contract actually says.
   - If a field is not mentioned anywhere in the contract, return null for it.

Return ONLY valid JSON, no markdown:

  ""nameMatchScore"":          <0-100>,
  ""authenticityScore"":       <0-100>,
  ""overallConfidenceScore"":  <0-100>,
  ""fakeDetectionFlag"":       <true|false>,
  ""recommendation"":          ""AUTO_VERIFY|MANUAL_REVIEW|REJECT"",
  ""analysisNotes"":           ""..."",
  ""extractedCountry"":        ""..."" | null,
  ""extractedGovernorate"":    ""..."" | null,
  ""extractedCity"":           ""..."" | null,
  ""extractedDistrict"":       ""..."" | null,
  ""extractedAddress"":        ""..."" | null

";
        }

        /// <summary>
        /// Parse Groq's JSON response into AiVerificationResult.
        /// </summary>
        private AiVerificationResult ParseClaudeResponse(
            string jsonResponse,
            string extractedOwnerName,
            string? extractedIdExpiry
        )
        {
            try
            {
                // Clean JSON response (remove markdown code blocks if present)
                var cleanJson = jsonResponse
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();

                var json = JsonDocument.Parse(cleanJson);
                var root = json.RootElement;

                static string? ReadNullableString(JsonElement root, string key)
                {
                    if (!root.TryGetProperty(key, out var el)) return null;
                    return el.ValueKind == JsonValueKind.Null ? null : el.GetString();
                }

                var result = new AiVerificationResult
                {
                    NameMatchScore = root.GetProperty("nameMatchScore").GetDecimal(),
                    AuthenticityScore = root.GetProperty("authenticityScore").GetDecimal(),
                    ConfidenceScore = root.GetProperty("overallConfidenceScore").GetDecimal(),
                    FakeDetectionFlag = root.GetProperty("fakeDetectionFlag").GetBoolean(),
                    Recommendation = root.GetProperty("recommendation").GetString() ?? "",
                    AnalysisNotes = root.GetProperty("analysisNotes").GetString() ?? "",
                    ExtractedOwnerName = extractedOwnerName,
                    ExtractedIdExpiry = extractedIdExpiry ?? "",
                    ExtractedCountry = ReadNullableString(root, "extractedCountry"),
                    ExtractedGovernorate = ReadNullableString(root, "extractedGovernorate"),
                    ExtractedCity = ReadNullableString(root, "extractedCity"),
                    ExtractedDistrict = ReadNullableString(root, "extractedDistrict"),
                    ExtractedAddress = ReadNullableString(root, "extractedAddress"),
                };

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing Groq response: {Response}", jsonResponse);

                // Return default low-confidence result if parsing fails
                return new AiVerificationResult
                {
                    NameMatchScore = 0,
                    AuthenticityScore = 0,
                    ConfidenceScore = 0,
                    FakeDetectionFlag = true,
                    Recommendation = "MANUAL_REVIEW",
                    AnalysisNotes = $"Failed to parse Groq response: {ex.Message}",
                    ExtractedOwnerName = extractedOwnerName,
                    ExtractedIdExpiry = extractedIdExpiry ?? ""
                };
            }
        }

        /// <summary>
        /// Helper: Combine first and last name from ID data.
        /// </summary>
        private string GetFullName(Dictionary<string, string> idData)
        {
            var first = idData.TryGetValue("FirstName", out var f) ? f : "";
            var last = idData.TryGetValue("LastName", out var l) ? l : "";

            return $"{first} {last}".Trim();
        }

    // ─────────────────────────────────────────────────────────────────────────
// Location comparison (deterministic, runs in C# — not Groq)
// ─────────────────────────────────────────────────────────────────────────

private LocationMatchDetails ScoreLocation(
    AiVerificationResult ai,
    PropertyLocationDto expected)
        {
            var details = new LocationMatchDetails
            {
                CountryScore = LocationScorer.Similarity(ai.ExtractedCountry, expected.Country),
                GovernorateScore = LocationScorer.Similarity(ai.ExtractedGovernorate, expected.Governorate),
                CityScore = LocationScorer.Similarity(ai.ExtractedCity, expected.City),
                DistrictScore = LocationScorer.Similarity(ai.ExtractedDistrict, expected.District),
                AddressScore = LocationScorer.Similarity(ai.ExtractedAddress, expected.AddressLine),
            };

            var notes = new List<string>();
            if (details.CountryScore < 60) notes.Add("Country mismatch");
            if (details.GovernorateScore < 60) notes.Add("Governorate mismatch");
            if (details.CityScore < 60) notes.Add("City mismatch");
            if (details.DistrictScore < 50) notes.Add("District mismatch");
            if (details.AddressScore < 50) notes.Add("Address very different");

            details.Notes = notes.Count == 0
                ? "All location fields aligned"
                : string.Join("; ", notes);

            return details;
        }

        private void AdjustConfidenceForLocation(AiVerificationResult r)
        {
            // Cap confidence based on location match quality
            r.ConfidenceScore = Math.Min(r.ConfidenceScore, r.LocationMatchScore + 20);

            if (r.LocationMatchScore < _verificationOptions.LocationRejectThreshold)
            {
                // Force REJECT only for confirmed mismatches (score > 0).
                // A score of exactly 0 usually means the contract omitted the location or
                // it could not be compared (e.g. language mismatch) — escalate to manual
                // review rather than silently rejecting a valid document.
                if (r.LocationMatchScore == 0)
                {
                    if (r.Recommendation == "AUTO_VERIFY")
                        r.Recommendation = "MANUAL_REVIEW";
                    r.AnalysisNotes +=
                        $"\n[Location] Manual review — location could not be verified " +
                        $"(score 0/100, likely not present in contract or language mismatch). {r.LocationMatch?.Notes}";
                }
                else
                {
                    r.Recommendation = "REJECT";
                    // NOTE: FakeDetectionFlag is intentionally NOT set here — a wrong address
                    // is a mismatch, not evidence of document forgery.
                    r.AnalysisNotes +=
                        $"\n[Location] REJECTED — confirmed location mismatch " +
                        $"({r.LocationMatchScore:F0}/100). {r.LocationMatch?.Notes}";
                }
            }
            else if (r.LocationMatchScore < _verificationOptions.LocationManualReviewThreshold)
            {
                if (r.Recommendation == "AUTO_VERIFY")
                    r.Recommendation = "MANUAL_REVIEW";

                r.AnalysisNotes +=
                    $"\n[Location] Forced manual review — partial location match " +
                    $"({r.LocationMatchScore:F0}/100). {r.LocationMatch?.Notes}";
            }
            else
            {
                r.AnalysisNotes +=
                    $"\n[Location] OK — match score {r.LocationMatchScore:F0}/100.";
            }
        }
    }
}
