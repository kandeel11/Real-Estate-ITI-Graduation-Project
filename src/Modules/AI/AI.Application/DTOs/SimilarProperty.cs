// AI.Application/DTOs/SimilarProperty.cs

namespace AI.Application.DTOs;

public sealed record SimilarProperty(
    Guid PropertyId,
    Guid OwnerId,
    string Title,
    string City,
    string District,
    string Street,
    double Score,           // 0.0–1.0 cosine similarity
    string MatchedFields    // "city, district, type, area, floor, unit"
);