// AI.Application/DTOs/DuplicateCheckResult.cs

namespace AI.Application.DTOs;

public sealed record DuplicateCheckResult(
    bool IsDuplicate,
    double HighestSimilarityScore,   // how duplicate-like (cosine similarity to nearest listing)
    double DecisionConfidence,       // how sure the engine/AI is in the verdict (0-1)
    double Threshold,
    IReadOnlyList<SimilarProperty> SimilarProperties,
    string Reasoning,
    string AdminNote         // pre-written note for admin review
);