// AI.Application/DTOs/DuplicateCheckResult.cs

namespace AI.Application.DTOs;

public sealed record DuplicateCheckResult(
    bool IsDuplicate,
    double HighestSimilarityScore,
    double Threshold,
    IReadOnlyList<SimilarProperty> SimilarProperties,
    string Reasoning,
    string AdminNote         // pre-written note for admin review
);