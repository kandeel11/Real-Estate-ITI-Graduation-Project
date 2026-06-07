using Domain.Entities;
using Identity.Domain.Events;

namespace Identity.Domain.Entities;

public class Company : BaseEntity
{
    public Guid ManagerId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string CommercialRegister { get; private set; } = string.Empty;
    public string? LogoUrl { get; private set; }
    public string? Website { get; private set; }
    public string? Description { get; private set; }
    public string City { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public bool IsVerified { get; private set; }
    public bool IsActive { get; private set; } = true;
    public decimal RatingAvg { get; private set; }
    public int TotalRatings { get; private set; }

    private Company() { }

    public static Company Create(
        Guid managerId,
        string name,
        string commercialRegister,
        string city,
        string address,
        string phone,
        string email,
        string? logoUrl = null,
        string? website = null,
        string? description = null)
    {
        var company = new Company
        {
            ManagerId = managerId,
            Name = name.Trim(),
            CommercialRegister = commercialRegister.Trim(),
            City = city.Trim(),
            Address = address.Trim(),
            Phone = phone,
            Email = email.Trim().ToLowerInvariant(),
            LogoUrl = logoUrl,
            Website = website,
            Description = description,
            IsVerified = false,
            IsActive = true
        };

        company.AddDomainEvent(new CompanyRegisteredEvent(company.Id, managerId));
        return company;
    }

    public void Verify() => IsVerified = true;

    public void Deactivate() => IsActive = false;

    public void UpdateRating(decimal avg, int total)
    {
        RatingAvg = avg;
        TotalRatings = total;
        MarkAsUpdated();
    }

    public void UpdateDetails(string name, string city, string address, string phone, string? website, string? description, string? logoUrl)
    {
        Name = name.Trim();
        City = city.Trim();
        Address = address.Trim();
        Phone = phone;
        Website = website;
        Description = description;
        LogoUrl = logoUrl;
        MarkAsUpdated();
    }
}
