using Domain.Entities;

namespace Identity.Domain.Entities;

public class Broker : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid? CompanyId { get; private set; }
    public string LicenseNumber { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public decimal AiPerformanceScore { get; private set; }
    public int ClosedDealsCount { get; private set; }
    public int AvgResponseTimeMinutes { get; private set; }
    public decimal ConversionRate { get; private set; }
    public decimal RatingAvg { get; private set; }
    public int TotalRatings { get; private set; }

    private Broker() { }

    public static Broker Create(Guid userId, Guid? companyId, string licenseNumber)
    {
        return new Broker
        {
            UserId = userId,
            CompanyId = companyId,
            LicenseNumber = licenseNumber.Trim(),
            IsActive = true
        };
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    public void UpdateAiPerformanceScore(decimal score)
    {
        AiPerformanceScore = score;
        MarkAsUpdated();
    }

    public void UpdateRating(decimal avg, int total)
    {
        RatingAvg = avg;
        TotalRatings = total;
        MarkAsUpdated();
    }
}
