// AI.Domain/Entities/LeaseExtractionRequest.cs

namespace AI.Domain.Entities;

public sealed class LeaseExtractionRequest
{
    public Guid Id { get; private set; }
    public Guid LeaseId { get; private set; }
    public string Status { get; private set; } = default!;
    public decimal? OverallConfidence { get; private set; }
    public string? RawAiResponse { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private LeaseExtractionRequest() { }

    public static LeaseExtractionRequest CreatePending(Guid leaseId) => new()
    {
        Id = Guid.NewGuid(),
        LeaseId = leaseId,
        Status = "pending",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    public void MarkCompleted(decimal confidence, string rawResponse)
    {
        Status = "completed";
        OverallConfidence = confidence;
        RawAiResponse = rawResponse;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string error)
    {
        Status = "failed";
        ErrorMessage = error;
        UpdatedAt = DateTime.UtcNow;
    }
}