// AI.Application/Events/PropertyDuplicateCheckCompletedEvent.cs

using AI.Application.DTOs;
using MediatR;

namespace AI.Application.Events;

public sealed record PropertyDuplicateCheckCompletedEvent(
    Guid PropertyId,
    bool IsDuplicate,
    double HighestSimilarityScore,
    IReadOnlyList<SimilarProperty> SimilarProperties,
    string Reasoning,
    string AdminNote
) : INotification;