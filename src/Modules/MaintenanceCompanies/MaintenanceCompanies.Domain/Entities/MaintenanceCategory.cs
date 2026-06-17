namespace MaintenanceCompanies.Domain.Entities;

public class MaintenanceCategory
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;       // "Plumbing"
    public string? Icon { get; private set; }                // "wrench"
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public ICollection<CompanyCategory> Companies { get; private set; } = new List<CompanyCategory>();

    // Constructor فاضي عشان EF Core يقدر يشتغل
    private MaintenanceCategory() { }

    // دالة الإنشاء الاحترافية (Factory Method)
    public static MaintenanceCategory Create(string name, string? icon, string? description)
    {
        return new MaintenanceCategory
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(), // بنعمل Trim عشان النظافة
            Icon = icon,
            Description = description,
            IsActive = true, // افتراضياً أي قسم جديد بيكون نشط
            CreatedAt = DateTime.UtcNow
        };
    }
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    // دالة التعديل (عشان لما نيجي نعمل الـ Update Command بعدين)
    public void UpdateDetails(string name, string? icon, string? description, bool isActive)
    {
        Name = name.Trim();
        Icon = icon;
        Description = description;
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }
}