using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Infrastructure.Persistence;
using MaintenanceCompanies.Domain.Entities;
using MaintenanceCompanies.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;
using Subscription.Application.Interfaces;

using IdentityMC = Identity.Domain.Entities.MaintenanceCompany;
using ModuleMC  = MaintenanceCompanies.Domain.Entities.MaintenanceCompany;

namespace RealEstate.Host.Seeds;

public static class TenantAndMaintenanceCompanySeed
{
    private static readonly string[] TenantEmails =
    [
        "sara.ahmed@seeddata.com",
        "mohamed.ali@seeddata.com",
        "fatima.nour@seeddata.com",
        "khaled.ibrahim@seeddata.com",
    ];

    private const string McEmail = "alfares.maintenance@seeddata.com";

    private static readonly string[] AllSeedEmails =
    [
        "sara.ahmed@seeddata.com",
        "mohamed.ali@seeddata.com",
        "fatima.nour@seeddata.com",
        "khaled.ibrahim@seeddata.com",
        "alfares.maintenance@seeddata.com",
    ];

    // ── Category definitions ─────────────────────────────────────────────────
    private static readonly (string Name, string Icon, string Description)[] CategoryDefs =
    [
        ("Plumbing",           "wrench",       "Water pipes, leaks, faucets, drainage, and water heaters"),
        ("Electrical",         "bolt",         "Wiring, circuit breakers, outlets, lighting, and electrical panels"),
        ("HVAC",               "snowflake",    "Air conditioning, heating, ventilation, and duct cleaning"),
        ("Painting",           "paintbrush",   "Interior and exterior painting, wall patching, and decorative finishes"),
        ("Carpentry",          "hammer",       "Furniture repair, door fitting, cabinets, and custom woodwork"),
        ("Tiling & Flooring",  "grid",         "Ceramic tiles, marble, parquet, and vinyl flooring installation"),
        ("Appliance Repair",   "cpu",          "Washing machines, refrigerators, ovens, and dishwasher repairs"),
        ("Pest Control",       "bug",          "Insect and rodent extermination, fumigation, and prevention"),
        ("Cleaning Services",  "sparkles",     "Deep cleaning, post-construction cleaning, and regular housekeeping"),
        ("Security Systems",   "shield",       "CCTV installation, alarm systems, access control, and intercom"),
        ("Roofing",            "home",         "Roof waterproofing, repair, insulation, and leak sealing"),
        ("Masonry",            "layers",       "Concrete work, wall construction, plastering, and crack repair"),
    ];

    // Returns true when at least one seed user is already in the DB.
    public static Task<bool> IsSeededAsync(IdentityDbContext identityDb)
        => identityDb.Users.AnyAsync(u => AllSeedEmails.Contains(u.Email));

    // ── Main seed ────────────────────────────────────────────────────────────
    public static async Task SeedAsync(
        IdentityDbContext identityDb,
        MaintenanceModuleDbContext maintenanceDb,
        IPasswordHasher hasher)
    {
        if (await identityDb.Users.AnyAsync(u => AllSeedEmails.Contains(u.Email)))
            return;

        // ── 1. Four Tenant users ─────────────────────────────────────────────
        var tenant1 = MakeVerifiedUser("Sara Ahmed Mahmoud",    "sara.ahmed@seeddata.com",     "+201100000001", UserRole.Tenant, hasher);
        var tenant2 = MakeVerifiedUser("Mohamed Ali Hassan",    "mohamed.ali@seeddata.com",    "+201100000002", UserRole.Tenant, hasher);
        var tenant3 = MakeVerifiedUser("Fatima Nour Eldin",     "fatima.nour@seeddata.com",    "+201100000003", UserRole.Tenant, hasher);
        var tenant4 = MakeVerifiedUser("Khaled Ibrahim Yousef", "khaled.ibrahim@seeddata.com", "+201100000004", UserRole.Tenant, hasher);

        await identityDb.Users.AddRangeAsync(tenant1, tenant2, tenant3, tenant4);
        await identityDb.SaveChangesAsync();

        // ── 2. Maintenance Company user ──────────────────────────────────────
        var mcUser = MakeVerifiedUser(
            "Al-Fares Maintenance Services",
            McEmail,
            "+201200000005",
            UserRole.MaintenanceCompany,
            hasher);

        await identityDb.Users.AddAsync(mcUser);
        await identityDb.SaveChangesAsync();

        // ── 3. Identity mirror record for the maintenance company ────────────
        var serviceNames = CategoryDefs.Select(c => c.Name).ToArray();

        var identityMc = IdentityMC.Create(
            userId: mcUser.Id,
            companyName: "Al-Fares Maintenance Services",
            serviceTypes: serviceNames,
            email: McEmail,
            phone: "+201200000005",
            city: "Cairo");

        await identityDb.MaintenanceCompanies.AddAsync(identityMc);
        await identityDb.SaveChangesAsync();

        // ── 4. Ensure all 12 categories exist in the MaintenanceCompanies module ──
        var existingNames = await maintenanceDb.MaintenanceCategories
            .Select(c => c.Name)
            .ToListAsync();

        var newCategories = CategoryDefs
            .Where(d => !existingNames.Contains(d.Name))
            .Select(d => MaintenanceCategory.Create(d.Name, d.Icon, d.Description))
            .ToList();

        if (newCategories.Count > 0)
        {
            await maintenanceDb.MaintenanceCategories.AddRangeAsync(newCategories);
            await maintenanceDb.SaveChangesAsync();
        }

        var allCategoryNames = CategoryDefs.Select(d => d.Name).ToList();
        var allCategories = await maintenanceDb.MaintenanceCategories
            .Where(c => allCategoryNames.Contains(c.Name))
            .ToListAsync();

        // ── 5. MaintenanceCompanies module record ────────────────────────────
        var adminId = await identityDb.Users
            .Where(u => u.Email == "admin@realestate.com")
            .Select(u => u.Id.ToString())
            .FirstOrDefaultAsync() ?? Guid.Empty.ToString();

        var mc = ModuleMC.Create(
            userId: mcUser.Id.ToString(),
            adminId: adminId,
            companyName: "Al-Fares Maintenance Services",
            serviceTypes: allCategories.Select(c => c.Name).ToArray(),
            email: McEmail,
            phone: "+201200000005",
            city: "Cairo",
            serviceRadiusKm: 40,
            description: "Full-service property maintenance company covering residential and commercial repair needs across Cairo and Giza. Trusted by hundreds of property owners since 2015.");

        foreach (var cat in allCategories)
            mc.Categories.Add(new CompanyCategory { CategoryId = cat.Id });

        // Working hours: Sun closed, Fri 9-14, Mon-Thu & Sat 8-18
        for (var i = 0; i < 7; i++)
        {
            var day     = (DayOfWeek)i;
            var closed  = day == DayOfWeek.Sunday;
            var friday  = day == DayOfWeek.Friday;
            mc.WorkingHours.Add(new CompanyWorkingHour
            {
                Day       = day,
                IsClosed  = closed,
                OpenTime  = closed ? null : new TimeOnly(friday ? 9 : 8, 0),
                CloseTime = closed ? null : new TimeOnly(friday ? 14 : 18, 0),
            });
        }

        maintenanceDb.MaintenanceCompanies.Add(mc);
        await maintenanceDb.SaveChangesAsync();
    }

    // ── Backfill FreeTenant subscriptions for the 4 seed tenants ────────────
    // Idempotent: skips tenants that already have an active subscription.
    public static async Task BackfillTenantSubscriptionsAsync(
        IdentityDbContext identityDb,
        ISubscriptionRepository subscriptions,
        IPublisher publisher)
    {
        var tenants = await identityDb.Users
            .Where(u => TenantEmails.Contains(u.Email))
            .ToListAsync();

        if (tenants.Count == 0) return;

        foreach (var tenant in tenants)
        {
            if (await subscriptions.GetActiveByUserAsync(tenant.Id) is null)
                await publisher.Publish(new IndividualUserCreatedEvent(tenant.Id, "Tenant"));
        }
    }

    // ── Factory ───────────────────────────────────────────────────────────────
    private static User MakeVerifiedUser(
        string fullName, string email, string phone, UserRole role, IPasswordHasher hasher)
    {
        var user = User.Create(fullName, email, hasher.Hash("Seed@12345"), phone, role);
        user.VerifyEmail(user.EmailVerificationToken!);
        user.ClearDomainEvents();
        return user;
    }
}
