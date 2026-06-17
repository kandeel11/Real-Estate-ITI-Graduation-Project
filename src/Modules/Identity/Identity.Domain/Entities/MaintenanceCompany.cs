using Domain.Entities;

namespace Identity.Domain.Entities;

public class MaintenanceCompany : BaseEntity
{
    public Guid UserId { get; private set; }
    public string CompanyName { get; private set; } = string.Empty;
    public string ServiceTypes { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public decimal RatingAvg { get; private set; }
    public int TotalRatings { get; private set; }

    private MaintenanceCompany() { }

    public static MaintenanceCompany Create(
        Guid userId,
        string companyName,
        string[] serviceTypes,
        string email,
        string phone,
        string city)
    {
        return new MaintenanceCompany
        {
            UserId = userId,
            CompanyName = companyName.Trim(),
            ServiceTypes = string.Join(",", serviceTypes),
            Email = email.Trim().ToLowerInvariant(),
            Phone = phone,
            City = city.Trim(),
            IsActive = true
        };
    }

    public string[] GetServiceTypes() =>
        ServiceTypes.Split(',', StringSplitOptions.RemoveEmptyEntries);

    public void Activate()
    {
        IsActive = true;
        MarkAsUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }

    public void UpdateRating(decimal avg, int total)
    {
        RatingAvg = avg;
        TotalRatings = total;
        MarkAsUpdated();
    }
}