using AI.Application.Features.OwnershipVerification.DTOs;
using AI.Application.Features.OwnershipVerification.Interfaces;
using AI.Application.Common.Options;
using AI.Application.Interfaces.Common;
using Azure;
using Azure.AI.DocumentIntelligence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;

namespace AI.Infrastructure.Services
{
    /// <summary>
    /// Implementation of IOwnershipAiService using Azure Document Intelligence + Claude text analysis.
    /// 
    /// Pipeline:
    /// 1. Azure DI prebuilt-idDocument → Extract Name, ID#, Expiry (structured)
    /// 2. Azure DI prebuilt-layout → Extract text from ownership proof/contract
    /// 3. Claude text model → Compare names, verify authenticity, return scores
    /// </summary>
    public class AzureDocumentIntelligenceOwnershipAiService : IOwnershipAiService
    {
        private readonly DocumentIntelligenceClient _documentClient;
        private readonly Kernel _kernel;
        private readonly ILogger<AzureDocumentIntelligenceOwnershipAiService> _logger;
        private readonly VerificationOptions _verificationOptions;

        public AzureDocumentIntelligenceOwnershipAiService(
            IOptions<AzureDocumentIntelligenceOptions> diOptions,
            Kernel kernel,
            ILogger<AzureDocumentIntelligenceOwnershipAiService> logger,
            IOptions<VerificationOptions> verificationOptions
        )
        {
            var opt = diOptions.Value;
            _documentClient = new DocumentIntelligenceClient(
                new Uri(opt.Endpoint),
                new AzureKeyCredential(opt.ApiKey)
            );
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
                var idData = await ExtractNationalIdDataAsync(nationalIdFile, ct);
                var ownershipText = await ExtractOwnershipProofTextAsync(ownershipProofFile, ct);

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
        /// Extract structured data from National ID using Azure DI prebuilt-idDocument.
        /// Returns: Name, ID Number, Expiry Date
        /// </summary>
        private async Task<Dictionary<string, string>> ExtractNationalIdDataAsync(
            byte[] fileContent,
            CancellationToken ct
        )
        {
            var result = new Dictionary<string, string>();

            try
            {
                var operation = await _documentClient.AnalyzeDocumentAsync(
                    WaitUntil.Completed,
                    "prebuilt-idDocument",
                    BinaryData.FromBytes(fileContent),
                    cancellationToken: ct
                );

                var analyzeResult = operation.Value;

                if (analyzeResult.Documents == null || analyzeResult.Documents.Count == 0)
                {
                    _logger.LogWarning("No ID document detected");
                    return result;
                }

                var doc = analyzeResult.Documents[0];

                // Extract fields
                if (doc.Fields.TryGetValue("FirstName", out var firstName))
                    result["FirstName"] = firstName.ValueString ?? "";

                if (doc.Fields.TryGetValue("LastName", out var lastName))
                    result["LastName"] = lastName.ValueString ?? "";

                if (doc.Fields.TryGetValue("DocumentNumber", out var idNumber))
                    result["DocumentNumber"] = idNumber.ValueString ?? "";

                if (doc.Fields.TryGetValue("ExpirationDate", out var expiry))
                    result["ExpirationDate"] = expiry.ValueDate?.ToString("yyyy-MM-dd") ?? "";

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
        /// Extract clean text from ownership proof (contract/deed) using Azure DI prebuilt-layout.
        /// Returns first 3 pages of text content.
        /// </summary>
        private async Task<string> ExtractOwnershipProofTextAsync(
            byte[] fileContent,
            CancellationToken ct
        )
        {
            try
            {
                var operation = await _documentClient.AnalyzeDocumentAsync(
                    WaitUntil.Completed,
                    "prebuilt-layout",
                    BinaryData.FromBytes(fileContent),
                    cancellationToken: ct
                );

                var result = operation.Value;

                var textContent = new System.Text.StringBuilder();

                // Limit to first 3 pages
                var pagesToProcess = result.Pages?.Take(3) ?? [];

                foreach (var page in pagesToProcess)
                {
                    var paragraphsOnPage = result.Paragraphs?
                        .Where(p => p.BoundingRegions?.Any(r => r.PageNumber == page.PageNumber) ?? false)
                        .ToList() ?? [];

                    foreach (var paragraph in paragraphsOnPage)
                    {
                        // Skip headers/footers
                        if (paragraph.Role != "pageHeader" &&
                            paragraph.Role != "pageFooter" &&
                            paragraph.Role != "pageNumber")
                        {
                            textContent.AppendLine(paragraph.Content);
                        }
                    }
                }

                var text = textContent.ToString().Trim();
                _logger.LogInformation("Extracted {Length} chars from ownership proof", text.Length);

                return text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting ownership proof text");
                throw;
            }
        }

        /// <summary>
        /// Use Claude to perform semantic analysis and comparison.
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

            // Build prompt for Claude
            var prompt = BuildAnalysisPrompt(submittedOwnerName, extractedOwnerName, extractedIdExpiry, ownershipProofText, expectedLocation);

            _logger.LogInformation("Sending analysis prompt to Claude");

            // Call Claude text model via Semantic Kernel
            var function = _kernel.CreateFunctionFromPrompt(prompt);
            var result = await _kernel.InvokeAsync(function, cancellationToken: ct);

            var responseText = result.ToString();
            _logger.LogInformation("Claude response: {Response}", responseText);

            // Parse Claude's JSON response
            var analysisResult = ParseClaudeResponse(responseText, extractedOwnerName, extractedIdExpiry);

            return analysisResult;
        }

        /// <summary>
        /// Build a structured prompt for Claude to analyze ownership documents.
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
   - Look in both Arabic and English. Egyptian contracts often write 'محافظة', 'مدينة', 'حي', 'شارع', 'عنوان'.
   - Do NOT just copy the expected location; only extract what the contract actually says.
   - If a field is not mentioned in the contract, return null for it.

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
        /// Parse Claude's JSON response into AiVerificationResult.
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

                var result = new AiVerificationResult
                {
                    NameMatchScore = root.GetProperty("nameMatchScore").GetDecimal(),
                    AuthenticityScore = root.GetProperty("authenticityScore").GetDecimal(),
                    ConfidenceScore = root.GetProperty("overallConfidenceScore").GetDecimal(),
                    FakeDetectionFlag = root.GetProperty("fakeDetectionFlag").GetBoolean(),
                    Recommendation = root.GetProperty("recommendation").GetString() ?? "",
                    AnalysisNotes = root.GetProperty("analysisNotes").GetString() ?? "",
                    ExtractedOwnerName = extractedOwnerName,
                    ExtractedIdExpiry = extractedIdExpiry ?? ""
                };

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing Claude response: {Response}", jsonResponse);

                // Return default low-confidence result if parsing fails
                return new AiVerificationResult
                {
                    NameMatchScore = 0,
                    AuthenticityScore = 0,
                    ConfidenceScore = 0,
                    FakeDetectionFlag = true,
                    Recommendation = "MANUAL_REVIEW",
                    AnalysisNotes = $"Failed to parse Claude response: {ex.Message}",
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
// Location comparison (deterministic, runs in C# — not Claude)
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
            // Final confidence is capped by location match
            r.ConfidenceScore = Math.Min(r.ConfidenceScore, r.LocationMatchScore + 20);

            if (r.LocationMatchScore < _verificationOptions.LocationRejectThreshold)
            {
                r.Recommendation = "REJECT";
                r.FakeDetectionFlag = true;
                r.AnalysisNotes +=
                    $"\n[Location] REJECTED — location mismatch " +
                    $"({r.LocationMatchScore:F0}/100). {r.LocationMatch?.Notes}";
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
