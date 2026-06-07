using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Seed;

public static class AdminSeed
{
    public static async Task SeedAsync(IdentityDbContext context, IPasswordHasher hasher)
    {
        const string adminEmail = "admin@realestate.com";

        if (await context.Users.AnyAsync(u => u.Email == adminEmail))
            return;

        var admin = User.Create(
            fullName: "Platform Admin",
            email: adminEmail,
            passwordHash: hasher.Hash("Admin@12345"),
            phone: "+201000000000",
            role: UserRole.Admin);

        // Auto-verify the admin account
        admin.VerifyEmail(admin.EmailVerificationToken!);

        context.Users.Add(admin);
        await context.SaveChangesAsync();
    }
}
