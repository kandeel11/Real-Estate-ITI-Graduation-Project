using Domain.Entities;

namespace Property.Domain.Entities;

public class Amenity : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string NameAr { get; private set; } = string.Empty;
    public string? IconUrl { get; private set; }
    public string Category { get; private set; } = string.Empty;

    private Amenity() { }

    public static Amenity Create(string name, string nameAr, string category, string? iconUrl = null)
    {
        return new Amenity
        {
            Name = name,
            NameAr = nameAr,
            Category = category,
            IconUrl = iconUrl
        };
    }
}
