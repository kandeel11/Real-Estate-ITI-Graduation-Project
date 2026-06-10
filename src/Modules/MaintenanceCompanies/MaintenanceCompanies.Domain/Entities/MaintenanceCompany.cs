using Domain.Entities; // Assuming BaseEntity is here

namespace MaintenanceCompanies.Domain.Entities;

public class MaintenanceCompany : BaseEntity
{
    // Linked Identity user (role = "MaintenanceCompany")
    public string UserId { get; private set; } = null!;

    public string CompanyName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string Phone { get; private set; } = null!;
    public string City { get; private set; } = null!;
    public string ServiceTypes { get; private set; } = string.Empty;
    public int ServiceRadiusKm { get; private set; } = 25;
    public string? LogoUrl { get; private set; }
    public string? Description { get; private set; }

    public bool IsActive { get; private set; } = true;
    public double AverageRating { get; private set; }
    public int CompletedTicketsCount { get; private set; }
    public int ActiveTicketsCount { get; private set; }

    public string CreatedByAdminId { get; private set; } = null!;

    // Navigation
    public ICollection<CompanyCategory> Categories { get; private set; } = new List<CompanyCategory>();
    public ICollection<CompanyWorkingHour> WorkingHours { get; private set; } = new List<CompanyWorkingHour>();
    public ICollection<CompanyReview> Reviews { get; private set; } = new List<CompanyReview>();

    // Empty constructor for EF Core
    private MaintenanceCompany() { }
    public void UpdateDetails(
        string companyName,
        string phone,
        string city,
        int serviceRadiusKm,
        string? description,
        string? logoUrl)
    {
        CompanyName = companyName.Trim();
        Phone = phone;
        City = city.Trim();
        ServiceRadiusKm = serviceRadiusKm;
        Description = description;
        LogoUrl = logoUrl;

        UpdatedAt = DateTime.UtcNow;
         MarkAsUpdated();
    }

    // Static Factory Method for Creation
    public static MaintenanceCompany Create(
        string userId,
        string adminId,
        string companyName,
        string[] serviceTypes,
        string email,
        string phone,
        string city,
        int serviceRadiusKm = 25,
        string? description = null)
    {
        return new MaintenanceCompany
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CreatedByAdminId = adminId,
            CompanyName = companyName.Trim(),
            ServiceTypes = string.Join(",", serviceTypes),
            Email = email.Trim().ToLowerInvariant(),
            Phone = phone,
            City = city.Trim(),
            ServiceRadiusKm = serviceRadiusKm,
            Description = description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    // Domain Behavior Methods
    public string[] GetServiceTypes() =>
        ServiceTypes.Split(',', StringSplitOptions.RemoveEmptyEntries);

    public void UpdateRating(double avgRating)
    {
        AverageRating = avgRating;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ToggleActiveStatus(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }
    public void IncrementActiveTickets()
    {
        ActiveTicketsCount++;
    }

    public void CompleteTicket()
    {
        if (ActiveTicketsCount > 0) ActiveTicketsCount--;
        CompletedTicketsCount++;
    }
}