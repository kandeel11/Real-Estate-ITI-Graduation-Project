using Microsoft.EntityFrameworkCore;
using Property.Domain.Entities;
using Property.Infrastructure.Persistence.Contexts;

namespace Property.Infrastructure.Persistence.Seeding;

public static class AmenitySeed
{
    public static async Task SeedAsync(PropertyDbContext context)
    {
        if (await context.Amenities.AnyAsync())
            return;

        var amenities = new[]
        {
            Amenity.Create("Swimming Pool", "حمام سباحة", "Pool"),
            Amenity.Create("Gym", "صالة رياضية", "Fitness"),
            Amenity.Create("Parking", "موقف سيارات", "Parking"),
            Amenity.Create("Security", "أمن", "Security"),
            Amenity.Create("CCTV", "كاميرات مراقبة", "Security"),
            Amenity.Create("Elevator", "مصعد", "Building"),
            Amenity.Create("Garden", "حديقة", "Outdoor"),
            Amenity.Create("Balcony", "شرفة", "Outdoor"),
            Amenity.Create("Central Air Conditioning", "تكييف مركزي", "Climate"),
            Amenity.Create("Intercom", "اتصال داخلي", "Building"),
            Amenity.Create("Kids Play Area", "منطقة ألعاب للأطفال", "Outdoor"),
            // "Pet Friendly" intentionally omitted — duplicated by the PetsAllowed property field.
            Amenity.Create("High-Speed Internet", "إنترنت فائق السرعة", "Utilities"),
            Amenity.Create("Natural Gas", "غاز طبيعي", "Utilities"),
            Amenity.Create("Water Heater", "سخان مياه", "Utilities"),
            Amenity.Create("Storage Room", "غرفة تخزين", "Building"),
            Amenity.Create("Maid's Room", "غرفة خادمة", "Building"),
            Amenity.Create("Sea / Nile View", "إطلالة على البحر / النيل", "Outdoor"),
        };

        await context.Amenities.AddRangeAsync(amenities);
        await context.SaveChangesAsync();
    }
}
