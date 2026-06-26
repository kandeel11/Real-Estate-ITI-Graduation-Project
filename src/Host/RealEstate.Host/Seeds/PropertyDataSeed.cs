using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Persistence.Contexts;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;
using Subscription.Application.Interfaces;

namespace RealEstate.Host.Seeds;

public static class PropertyDataSeed
{
    // All seed user emails — used both for idempotency and for targeted cleanup
    private static readonly string[] SeedEmails =
    [
        "ahmed.hassan@seeddata.com",
        "mona.ibrahim@seeddata.com",
        "youssef.gamal@seeddata.com",
        "omar.samy@seeddata.com",
        "nadia.khaled@seeddata.com",
        "karim.mostafa@seeddata.com",
        "dina.ashraf@seeddata.com",
        "hassan.ramzy@seeddata.com",
    ];

    private const string BlobContainer = "property-media";

    // Returns true when at least one seed user is already in the DB.
    public static Task<bool> IsSeededAsync(IdentityDbContext identityDb)
        => identityDb.Users.AnyAsync(u => SeedEmails.Contains(u.Email));

    // ─────────────────────────────────────────────────────────────────────────
    public static async Task SeedAsync(
        PropertyDbContext propertyDb,
        IdentityDbContext identityDb,
        IPasswordHasher hasher,
        BlobServiceClient? blobServiceClient,
        string? webRootPath)
    {
        // Run only once — if any seed user already exists, the DB is seeded; skip entirely.
        if (await identityDb.Users.AnyAsync(u => SeedEmails.Contains(u.Email)))
            return;

        // ── 1. Owner Users ────────────────────────────────────────────────────
        var ownerCairo   = MakeVerifiedUser("Ahmed Hassan Khalil",  "ahmed.hassan@seeddata.com",   "+201001001001", UserRole.Owner,          hasher);
        var ownerAlex    = MakeVerifiedUser("Mona Ibrahim Saad",    "mona.ibrahim@seeddata.com",   "+201002002002", UserRole.Owner,          hasher);
        var ownerTanta   = MakeVerifiedUser("Youssef Gamal Nasser", "youssef.gamal@seeddata.com",  "+201003003003", UserRole.Owner,          hasher);

        // ── 2. Company Manager Users ──────────────────────────────────────────
        var managerCairo = MakeVerifiedUser("Omar Samy Fawzy",      "omar.samy@seeddata.com",      "+201004004004", UserRole.CompanyManager, hasher);
        var managerAlex  = MakeVerifiedUser("Nadia Khaled Mansour", "nadia.khaled@seeddata.com",   "+201005005005", UserRole.CompanyManager, hasher);

        await identityDb.Users.AddRangeAsync(ownerCairo, ownerAlex, ownerTanta, managerCairo, managerAlex);
        await identityDb.SaveChangesAsync();

        // ── 3. Companies ──────────────────────────────────────────────────────
        var companyCairo = Company.Create(
            managerCairo.Id, "Cairo Elite Properties", "CR-2020-CAIRO-001",
            "Cairo", "15 Tahrir Square, Downtown Cairo", "+20224444001", "info@cairoelite.com",
            description: "Leading real estate agency in Cairo specializing in luxury apartments for rent.");
        companyCairo.Verify();
        companyCairo.ClearDomainEvents();

        var companyAlex = Company.Create(
            managerAlex.Id, "Alex Premier Realty", "CR-2020-ALEX-002",
            "Alexandria", "22 Corniche Road, Alexandria", "+20345555002", "info@alexpremier.com",
            description: "Alexandria's top rental agency offering premium sea-view and coastal apartments.");
        companyAlex.Verify();
        companyAlex.ClearDomainEvents();

        await identityDb.Companies.AddRangeAsync(companyCairo, companyAlex);
        await identityDb.SaveChangesAsync();

        managerCairo.SetCompany(companyCairo.Id);
        managerAlex.SetCompany(companyAlex.Id);
        await identityDb.SaveChangesAsync();

        // Free-plan assignment for these accounts is handled by
        // BackfillSubscriptionsAsync, which publishes the integration events only
        // when no active subscription exists (covers both fresh and existing DBs).

        // ── 4. Broker Users & Records ─────────────────────────────────────────
        var brokerUser1 = MakeVerifiedUser("Karim Mostafa Amin", "karim.mostafa@seeddata.com", "+201006006006", UserRole.Broker, hasher);
        var brokerUser2 = MakeVerifiedUser("Dina Ashraf Lotfy",  "dina.ashraf@seeddata.com",  "+201007007007", UserRole.Broker, hasher);
        var brokerUser3 = MakeVerifiedUser("Hassan Ramzy Adel",  "hassan.ramzy@seeddata.com", "+201008008008", UserRole.Broker, hasher);

        await identityDb.Users.AddRangeAsync(brokerUser1, brokerUser2, brokerUser3);
        await identityDb.SaveChangesAsync();

        var broker1 = Broker.Create(brokerUser1.Id, companyCairo.Id, "BRK-CAIRO-001");
        var broker2 = Broker.Create(brokerUser2.Id, companyCairo.Id, "BRK-CAIRO-002");
        var broker3 = Broker.Create(brokerUser3.Id, companyAlex.Id,  "BRK-ALEX-003");

        await identityDb.Brokers.AddRangeAsync(broker1, broker2, broker3);
        await identityDb.SaveChangesAsync();

        // ════════════════════════════════════════════════════════════════════════
        // CAIRO APARTMENTS  (Monthly rent 10,000 – 20,000 EGP)
        // ════════════════════════════════════════════════════════════════════════

        // P1 — Owner, Zamalek, 3BR, 15,000/mo — FEATURED
        await AddPropertyAsync(propertyDb, blobServiceClient, webRootPath, propertyIndex: 1,
            ownerCairo.Id, null, null,
            "Furnished Apartment for Rent in Zamalek", "شقة مفروشة للإيجار في الزمالك",
            "Stunning fully-furnished 3-bedroom apartment on the 8th floor in prestigious Zamalek. Panoramic Nile views, central AC, 24-hour security, and underground parking. Available immediately.",
            "شقة فاخرة مفروشة بالكامل ذات 3 غرف نوم في الطابق الثامن بحي الزمالك الراقي. إطلالة بانورامية على النيل، تكييف مركزي، أمن 24 ساعة وموقف سيارات تحت الأرض.",
            PropertyType.Apartment, 15_000, 3, 2, 175,
            floor: 8, totalFloors: 15, yearBuilt: 2019, isFurnished: true, petsAllowed: false,
            "Egypt", "Cairo", "15 Hassan Sabry St, Zamalek", null, "Cairo", "Zamalek",
            ListingSource.Owner, "Ahmed Hassan Khalil", featured: true);

        // P2 — Owner, New Cairo, 4BR, 20,000/mo
        await AddPropertyAsync(propertyDb, blobServiceClient, webRootPath, propertyIndex: 2,
            ownerCairo.Id, null, null,
            "Spacious Apartment for Rent in New Cairo", "شقة واسعة للإيجار في القاهرة الجديدة",
            "Premium 4-bedroom apartment in a prestigious New Cairo compound with gym, pool, and 24-hr security. Italian tiles and smart home automation throughout.",
            "شقة فاخرة ذات 4 غرف نوم في كمبوند راقٍ بالقاهرة الجديدة مع جيم ومسبح وأمن 24 ساعة. بلاط إيطالي وأنظمة منزل ذكي في كل مكان.",
            PropertyType.Apartment, 20_000, 4, 3, 210,
            floor: 3, totalFloors: 10, yearBuilt: 2021, isFurnished: true, petsAllowed: false,
            "Egypt", "Cairo", "Block 5, 90th Street, New Cairo", 8, "Cairo", "Fifth Settlement",
            ListingSource.Owner, "Ahmed Hassan Khalil", featured: false);

        // P3 — Company, Heliopolis, 2BR, 10,000/mo
        await AddPropertyAsync(propertyDb, blobServiceClient, webRootPath, propertyIndex: 3,
            brokerUser1.Id, companyCairo.Id, brokerUser1.Id,
            "Modern Apartment for Rent in Heliopolis", "شقة عصرية للإيجار في مصر الجديدة",
            "Well-maintained 2-bedroom apartment in Heliopolis. Open-plan living and dining, modern kitchen, balcony. Close to City Stars Mall and metro station.",
            "شقة ذات غرفتي نوم في شارع هادئ بمصر الجديدة. صالة وسفرة مفتوحة، مطبخ عصري، شرفة. قريبة من سيتي ستارز ومحطة المترو.",
            PropertyType.Apartment, 10_000, 2, 1, 115,
            floor: 4, totalFloors: 10, yearBuilt: 2018, isFurnished: false, petsAllowed: false,
            "Egypt", "Cairo", "7 Merghany Street", 12, "Cairo", "Heliopolis",
            ListingSource.Company, "Cairo Elite Properties", featured: false);

        // P4 — Company, Maadi, 3BR, 14,000/mo — FEATURED
        await AddPropertyAsync(propertyDb, blobServiceClient, webRootPath, propertyIndex: 4,
            brokerUser2.Id, companyCairo.Id, brokerUser2.Id,
            "Bright Apartment for Rent in Maadi", "شقة مشرقة للإيجار في المعادي",
            "Charming 3-bedroom apartment in a lush garden building in Maadi. High ceilings, parquet floors, large balcony. Walking distance to Maadi Corniche and international schools.",
            "شقة ساحرة ذات 3 غرف نوم في مبنى ذو حديقة بالمعادي. أسقف عالية، باركيه، شرفة كبيرة. قريبة من كورنيش المعادي والمدارس الدولية.",
            PropertyType.Apartment, 14_000, 3, 2, 160,
            floor: 2, totalFloors: 6, yearBuilt: 2010, isFurnished: false, petsAllowed: true,
            "Egypt", "Cairo", "18 Road 257, Maadi", null, "Cairo", "Maadi",
            ListingSource.Company, "Cairo Elite Properties", featured: true);

        // ════════════════════════════════════════════════════════════════════════
        // ALEXANDRIA APARTMENTS  (Monthly rent 10,000 – 20,000 EGP)
        // ════════════════════════════════════════════════════════════════════════

        // P5 — Owner, Montaza, 3BR, 13,000/mo — FEATURED
        await AddPropertyAsync(propertyDb, blobServiceClient, webRootPath, propertyIndex: 5,
            ownerAlex.Id, null, null,
            "Sea View Apartment for Rent in Montaza", "شقة بإطلالة بحرية للإيجار في المنتزه",
            "Stunning 3-bedroom apartment with direct Mediterranean sea views in Montaza. Large wrap-around balcony, 24-hr doorman, and dedicated parking space.",
            "شقة رائعة ذات 3 غرف نوم بإطلالة مباشرة على البحر المتوسط في المنتزه. شرفة كبيرة محيطة، بواب 24 ساعة وموقف سيارات خاص.",
            PropertyType.Apartment, 13_000, 3, 2, 155,
            floor: 5, totalFloors: 12, yearBuilt: 2017, isFurnished: false, petsAllowed: false,
            "Egypt", "Alexandria", "Corniche Road, Montaza Palace District", 8, "Alexandria", "Montaza",
            ListingSource.Owner, "Mona Ibrahim Saad", featured: true);

        // P6 — Owner, Stanley, 2BR, 11,000/mo
        await AddPropertyAsync(propertyDb, blobServiceClient, webRootPath, propertyIndex: 6,
            ownerAlex.Id, null, null,
            "Cozy Apartment for Rent in Stanley", "شقة مريحة للإيجار في ستانلي",
            "Neat 2-bedroom apartment steps from Stanley Beach with partial sea view. Newly painted, granite kitchen counters, and security intercom. Quiet residential street.",
            "شقة أنيقة ذات غرفتي نوم على خطوات من شاطئ ستانلي مع إطلالة بحرية جزئية. دهان جديد، طرابيز جرانيت وانتركوم أمان. شارع هادئ.",
            PropertyType.Apartment, 11_000, 2, 1, 110,
            floor: 3, totalFloors: 8, yearBuilt: 2014, isFurnished: false, petsAllowed: false,
            "Egypt", "Alexandria", "12 Ahmed Shawky Street, Stanley", 4, "Alexandria", "Stanley",
            ListingSource.Owner, "Mona Ibrahim Saad", featured: false);

        // P7 — Company, Smouha, 2BR, 10,500/mo
        await AddPropertyAsync(propertyDb, blobServiceClient, webRootPath, propertyIndex: 7,
            brokerUser3.Id, companyAlex.Id, brokerUser3.Id,
            "Modern Apartment for Rent in Smouha", "شقة عصرية للإيجار في سموحة",
            "Well-designed 2-bedroom apartment in the vibrant Smouha district. Modern fixtures, split ACs in all rooms, and a covered balcony. Near Sporting Club.",
            "شقة ذات غرفتي نوم في حي سموحة الحيوي. أدوات عصرية، تكييف في جميع الغرف وشرفة مسقوفة. قريبة من النادي الرياضي.",
            PropertyType.Apartment, 10_500, 2, 2, 125,
            floor: 3, totalFloors: 8, yearBuilt: 2020, isFurnished: false, petsAllowed: false,
            "Egypt", "Alexandria", "25 Victor Emmanuel Square", 5, "Alexandria", "Smouha",
            ListingSource.Company, "Alex Premier Realty", featured: false);

        // P8 — Company, Gleem, 3BR, 17,000/mo
        await AddPropertyAsync(propertyDb, blobServiceClient, webRootPath, propertyIndex: 8,
            brokerUser3.Id, companyAlex.Id, brokerUser3.Id,
            "Luxury Apartment for Rent in Gleem", "شقة فاخرة للإيجار في جليم",
            "Prestigious 3-bedroom apartment in the upscale Gleem district. European-style kitchen, en-suite master bedroom, two balconies with sea glimpse, 24-hr concierge.",
            "شقة فاخرة ذات 3 غرف نوم في حي جليم الراقي. مطبخ أوروبي، غرفة رئيسية مع حمام داخلي، شرفتان مع لمحة بحرية، كونسيرج 24 ساعة.",
            PropertyType.Apartment, 17_000, 3, 2, 180,
            floor: 7, totalFloors: 14, yearBuilt: 2022, isFurnished: true, petsAllowed: false,
            "Egypt", "Alexandria", "8 Gleem Corniche", null, "Alexandria", "Gleem",
            ListingSource.Company, "Alex Premier Realty", featured: false);

        // ════════════════════════════════════════════════════════════════════════
        // TANTA APARTMENTS  (Monthly rent 10,000 – 15,000 EGP)
        // ════════════════════════════════════════════════════════════════════════

        // P9 — Owner, El Geish, 3BR, 12,000/mo
        await AddPropertyAsync(propertyDb, blobServiceClient, webRootPath, propertyIndex: 9,
            ownerTanta.Id, null, null,
            "Apartment for Rent in Central Tanta", "شقة للإيجار في وسط طنطا",
            "Comfortable 3-bedroom apartment in central Tanta on a high floor. Split ACs in all rooms, modern kitchen, building intercom. Walking distance to Tanta University.",
            "شقة مريحة ذات 3 غرف نوم في وسط طنطا بطابق عالٍ. تكييف في جميع الغرف، مطبخ عصري وانتركوم مبنى. قريبة من جامعة طنطا.",
            PropertyType.Apartment, 12_000, 3, 2, 140,
            floor: 5, totalFloors: 7, yearBuilt: 2015, isFurnished: false, petsAllowed: false,
            "Egypt", "Gharbia", "18 El Geish Street", 3, "Tanta", "El Geish District",
            ListingSource.Owner, "Youssef Gamal Nasser", featured: false);

        // P10 — Owner, El Motasem, 3BR, 13,500/mo
        await AddPropertyAsync(propertyDb, blobServiceClient, webRootPath, propertyIndex: 10,
            ownerTanta.Id, null, null,
            "Bright Apartment for Rent in Tanta", "شقة مشرقة للإيجار في طنطا",
            "Spacious 3-bedroom apartment in a new building in Tanta. High-quality finishes, large kitchen with dining area, and a sunny balcony. Quiet residential area.",
            "شقة فسيحة ذات 3 غرف نوم في مبنى جديد في طنطا. تشطيبات عالية الجودة، مطبخ كبير مع سفرة وشرفة مشمسة. منطقة سكنية هادئة.",
            PropertyType.Apartment, 13_500, 3, 2, 150,
            floor: 2, totalFloors: 6, yearBuilt: 2022, isFurnished: false, petsAllowed: false,
            "Egypt", "Gharbia", "New Building, El Motasem Street", 5, "Tanta", "El Motasem District",
            ListingSource.Owner, "Youssef Gamal Nasser", featured: false);

        // P11 — Company, City Center, 2BR, 10,000/mo
        await AddPropertyAsync(propertyDb, blobServiceClient, webRootPath, propertyIndex: 11,
            brokerUser1.Id, companyCairo.Id, brokerUser1.Id,
            "Affordable Apartment for Rent in Downtown Tanta", "شقة بسعر مناسب للإيجار في وسط طنطا",
            "Good-value 2-bedroom apartment in Tanta's commercial core. Building security, close to major banks, government offices, and public transport. Ideal for working professionals.",
            "شقة ذات غرفتي نوم بقيمة جيدة في قلب المنطقة التجارية بطنطا. أمن مبنى وقريبة من البنوك والمصالح الحكومية والمواصلات. مثالية للمهنيين.",
            PropertyType.Apartment, 10_000, 2, 1, 100,
            floor: 1, totalFloors: 5, yearBuilt: 2012, isFurnished: false, petsAllowed: false,
            "Egypt", "Gharbia", "3 El Bahr Street", 7, "Tanta", "City Center",
            ListingSource.Company, "Cairo Elite Properties", featured: false);

        // P12 — Company, El Geish, 3BR, 14,000/mo
        await AddPropertyAsync(propertyDb, blobServiceClient, webRootPath, propertyIndex: 12,
            brokerUser2.Id, companyCairo.Id, brokerUser2.Id,
            "Semi-Furnished Apartment for Rent in Tanta", "شقة نصف مفروشة للإيجار في طنطا",
            "Modern 3-bedroom semi-furnished apartment in a recently completed tower. Italian-finish tiles, fitted kitchen with appliances, ACs in all rooms, covered parking.",
            "شقة نصف مفروشة حديثة ذات 3 غرف نوم في برج اكتمل مؤخراً. بلاط إيطالي، مطبخ مجهز، تكييف في جميع الغرف وموقف مسقوف.",
            PropertyType.Apartment, 14_000, 3, 2, 155,
            floor: 4, totalFloors: 8, yearBuilt: 2024, isFurnished: true, petsAllowed: false,
            "Egypt", "Gharbia", "25 El Geish Street", 12, "Tanta", "El Geish District",
            ListingSource.Company, "Cairo Elite Properties", featured: false);
    }

    // ── Backfill subscriptions for already-seeded accounts ───────────────────
    // Runs on every startup (independent of the once-only seed guard). For seed
    // companies/owners created before the seed published the integration events,
    // this assigns the missing Free plan so /api/subscriptions/mine stops 404ing.
    // Idempotent: skips any account that already has an active subscription.
    public static async Task BackfillSubscriptionsAsync(
        IdentityDbContext identityDb,
        ISubscriptionRepository subscriptions,
        IPublisher publisher)
    {
        var seedUsers = await identityDb.Users
            .Where(u => SeedEmails.Contains(u.Email))
            .ToListAsync();

        if (seedUsers.Count == 0) return;

        // Individual owners → personal Free Individual subscription
        foreach (var owner in seedUsers.Where(u => u.Role == UserRole.Owner.ToRoleString()))
        {
            if (await subscriptions.GetActiveByUserAsync(owner.Id) is null)
                await publisher.Publish(new IndividualUserCreatedEvent(owner.Id, "Owner"));
        }

        // Companies → company Free Company subscription (keyed by manager seed user)
        var seedUserIds = seedUsers.Select(u => u.Id).ToList();
        var seedCompanies = await identityDb.Companies
            .Where(c => seedUserIds.Contains(c.ManagerId))
            .ToListAsync();

        foreach (var company in seedCompanies)
        {
            if (await subscriptions.GetActiveByCompanyAsync(company.Id) is null)
                await publisher.Publish(new CompanyCreatedEvent(company.Id, company.ManagerId));
        }
    }

    // ── Clear all previous seed data ─────────────────────────────────────────
    private static async Task ClearSeedDataAsync(
        PropertyDbContext propertyDb,
        IdentityDbContext identityDb)
    {
        var seedUsers = await identityDb.Users
            .Where(u => SeedEmails.Contains(u.Email))
            .ToListAsync();

        if (seedUsers.Count == 0) return;

        var seedUserIds = seedUsers.Select(u => u.Id).ToList();

        // 1 — Remove property media then properties (same module, within PropertyDbContext)
        var seedProperties = await propertyDb.Properties
            .Where(p => seedUserIds.Contains(p.OwnerId))
            .ToListAsync();

        if (seedProperties.Count > 0)
        {
            var propIds = seedProperties.Select(p => p.Id).ToList();
            var seedMedia = await propertyDb.PropertyMedia
                .Where(m => propIds.Contains(m.PropertyId))
                .ToListAsync();

            propertyDb.PropertyMedia.RemoveRange(seedMedia);
            propertyDb.Properties.RemoveRange(seedProperties);
            await propertyDb.SaveChangesAsync();
        }

        // 2 — Remove brokers (references Users + Companies)
        var seedBrokers = await identityDb.Brokers
            .Where(b => seedUserIds.Contains(b.UserId))
            .ToListAsync();
        identityDb.Brokers.RemoveRange(seedBrokers);

        // 3 — Remove companies (Manager FK points to seed users)
        var seedCompanies = await identityDb.Companies
            .Where(c => seedUserIds.Contains(c.ManagerId))
            .ToListAsync();
        identityDb.Companies.RemoveRange(seedCompanies);

        // 4 — Remove seed users
        identityDb.Users.RemoveRange(seedUsers);

        await identityDb.SaveChangesAsync();
    }

    // ── Create one property and upload its 5 images ───────────────────────────
    private static async Task AddPropertyAsync(
        PropertyDbContext db,
        BlobServiceClient? blobClient,
        string? webRootPath,
        int propertyIndex,
        Guid ownerId,
        Guid? companyId,
        Guid? brokerId,
        string title,      string titleAr,
        string description, string descriptionAr,
        PropertyType type,
        decimal price,
        int bedrooms, int bathrooms, decimal area,
        int floor, int totalFloors, int yearBuilt,
        bool isFurnished, bool petsAllowed,
        string country, string governorate,
        string address, int? apartmentNumber,
        string city, string district,
        ListingSource source,
        string ownerFullName,
        bool featured)
    {
        var property = Property.Domain.Entities.Property.Create(
            ownerId, companyId, brokerId,
            title, titleAr, description, descriptionAr,
            type, price, bedrooms, bathrooms, area,
            floor, totalFloors, yearBuilt, isFurnished, petsAllowed,
            address, apartmentNumber, country, governorate,
            ownerShipStatus: "verified",
            ownerFullName: ownerFullName,
            nationalIdBlobPath: "seed/national-id-placeholder.pdf",
            ownershipProofBlobPath: "seed/ownership-proof-placeholder.pdf",
            city, district, source);

        property.SetVerificationStatus(Property.Domain.Enums.VerificationStatus.ManuallyVerified);
        if (featured)
            property.SetFeatured(true, DateTime.UtcNow.AddMonths(3));
        property.ClearDomainEvents();

        db.Properties.Add(property);
        await db.SaveChangesAsync();

        // Upload all 5 images; image index 1 is the primary
        for (int imgNum = 1; imgNum <= 5; imgNum++)
        {
            var localPath = BuildImagePath(webRootPath, propertyIndex, imgNum);
            var url = await UploadLocalImageAsync(blobClient, property.Id, localPath, propertyIndex, imgNum);

            var fileSizeKb = File.Exists(localPath)
                ? (int)(new FileInfo(localPath).Length / 1024)
                : 100;

            var media = PropertyMedia.Create(
                property.Id, url, MediaType.Image,
                fileSizeKb: fileSizeKb, sortOrder: imgNum - 1);

            media.SetPrimary(imgNum == 1); // first image is the primary thumbnail
            db.PropertyMedia.Add(media);
        }

        await db.SaveChangesAsync();
    }

    // ── Upload one local image file to Azure Blob Storage ────────────────────
    private static async Task<string> UploadLocalImageAsync(
        BlobServiceClient? blobClient,
        Guid propertyId,
        string localFilePath,
        int propertyIndex,
        int imageNumber)
    {
        // Fallback static-file URL (served from wwwroot if Azure is unavailable)
        var staticUrl = $"/ImageSeed/Property{propertyIndex}/Prop{propertyIndex}.{imageNumber}.jpg";

        if (!File.Exists(localFilePath))
            return staticUrl;

        if (blobClient is null)
            return staticUrl;

        try
        {
            var imageBytes = await File.ReadAllBytesAsync(localFilePath);

            var containerClient = blobClient.GetBlobContainerClient(BlobContainer);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            // blob name: {propertyId}/{imageNumber:D2}-photo.jpg
            var blobName = $"{propertyId}/{imageNumber:D2}-photo.jpg";
            var blobRef  = containerClient.GetBlobClient(blobName);

            using var stream = new MemoryStream(imageBytes);
            await blobRef.UploadAsync(stream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = "image/jpeg" }
            });

            return blobRef.Uri.ToString(); // full Azure CDN URL
        }
        catch
        {
            return staticUrl; // Azure unreachable — serve from wwwroot
        }
    }

    // ── Build the absolute path to a seed image file ──────────────────────────
    private static string BuildImagePath(string? webRootPath, int propertyIndex, int imageNumber)
    {
        if (string.IsNullOrEmpty(webRootPath))
            return string.Empty;

        return Path.Combine(
            webRootPath,
            "ImageSeed",
            $"Property{propertyIndex}",
            $"Prop{propertyIndex}.{imageNumber}.jpg");
    }

    // ── Factory: create a verified user, clearing seeding-noise domain events ──
    private static User MakeVerifiedUser(
        string fullName, string email, string phone, UserRole role, IPasswordHasher hasher)
    {
        var user = User.Create(fullName, email, hasher.Hash("Seed@12345"), phone, role);
        user.VerifyEmail(user.EmailVerificationToken!);
        user.ClearDomainEvents();
        return user;
    }
}
