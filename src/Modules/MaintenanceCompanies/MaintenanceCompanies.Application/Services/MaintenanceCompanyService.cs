using MaintenanceCompanies.Application.DTOs.Categories;
using MaintenanceCompanies.Application.DTOs.Companies;
using MaintenanceCompanies.Application.Interfaces;
using MaintenanceCompanies.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceCompanies.Application.Services;

public class MaintenanceCompanyService : IMaintenanceCompanyService
{
    private readonly IMaintenanceModuleDbContext _db;

    public MaintenanceCompanyService(IMaintenanceModuleDbContext db)
    {
        _db = db;
    }

    public async Task<List<MaintenanceCompanyListItemDto>> GetAllAsync(
        string? city, Guid? categoryId, bool? isActive)
    {
        var q = _db.MaintenanceCompanies
            .Include(c => c.Categories).ThenInclude(cc => cc.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(city))
            q = q.Where(c => c.City == city);

        if (categoryId.HasValue)
            q = q.Where(c => c.Categories.Any(cc => cc.CategoryId == categoryId.Value));

        if (isActive.HasValue)
            q = q.Where(c => c.IsActive == isActive.Value);

        return await q
            .OrderByDescending(c => c.AverageRating)
            .Select(c => new MaintenanceCompanyListItemDto(
                c.Id, c.CompanyName, c.City, c.IsActive,
                c.AverageRating, c.ActiveTicketsCount,
                c.Categories.Select(cc => cc.Category.Name).ToList()
            ))
            .ToListAsync();
    }

    public async Task<MaintenanceCompanyResponseDto?> GetByIdAsync(Guid id)
    {
        var c = await _db.MaintenanceCompanies
            .Include(x => x.Categories).ThenInclude(cc => cc.Category)
            .FirstOrDefaultAsync(x => x.Id == id);
        return c == null ? null : Map(c);
    }

    public async Task<MaintenanceCompanyResponseDto?> GetByUserIdAsync(string userId)
    {
        var c = await _db.MaintenanceCompanies
            .Include(x => x.Categories).ThenInclude(cc => cc.Category)
            .FirstOrDefaultAsync(x => x.UserId == userId);
        return c == null ? null : Map(c);
    }

    // تم التعديل: استقبال userId من الخارج بدلاً من إنشاء المستخدم هنا
    public async Task<MaintenanceCompanyResponseDto> CreateAsync(
        CreateMaintenanceCompanyDto dto, string userId, string adminId)
    {
        // 1. Validate categories exist
        var categories = await _db.MaintenanceCategories
            .Where(c => dto.CategoryIds.Contains(c.Id) && c.IsActive)
            .ToListAsync();

        if (categories.Count != dto.CategoryIds.Count)
            throw new InvalidOperationException("One or more categories are invalid or inactive.");

        // 2. Create company entity using the Domain Factory Method
        var company = MaintenanceCompany.Create(
            userId: userId,
            adminId: adminId,
            companyName: dto.CompanyName,
            serviceTypes: categories.Select(c => c.Name).ToArray(),
            email: dto.Email,
            phone: dto.Phone,
            city: dto.City,
            serviceRadiusKm: dto.ServiceRadiusKm,
            description: dto.Description
        );

        // Add Categories Mapping
        foreach (var cid in dto.CategoryIds)
        {
            company.Categories.Add(new CompanyCategory { CategoryId = cid });
        }

        // 3. Seed default working hours (Mon-Sat 9-17, Sun closed)
        for (int i = 0; i < 7; i++)
        {
            var day = (DayOfWeek)i;
            var closed = day == DayOfWeek.Sunday;
            company.WorkingHours.Add(new CompanyWorkingHour
            {
                Day = day,
                IsClosed = closed,
                OpenTime = closed ? null : new TimeOnly(9, 0),
                CloseTime = closed ? null : new TimeOnly(17, 0)
            });
        }

        _db.MaintenanceCompanies.Add(company);
        await _db.SaveChangesAsync();

        // Reload with category names for the response
        var saved = await _db.MaintenanceCompanies
            .Include(x => x.Categories).ThenInclude(cc => cc.Category)
            .FirstAsync(x => x.Id == company.Id);

        return Map(saved);
    }

    public async Task<MaintenanceCompanyResponseDto?> UpdateAsync(Guid id, UpdateMaintenanceCompanyDto dto)
    {
        var c = await _db.MaintenanceCompanies
            .Include(x => x.Categories)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (c == null) return null;

        // Note: For DDD, consider adding an 'UpdateDetails' method in the MaintenanceCompany entity later.
        c.UpdateDetails(
            dto.CompanyName,
            dto.Phone,
            dto.City,
            dto.ServiceRadiusKm,
            dto.Description,
            dto.LogoUrl
        );

        // Re-sync categories
        c.Categories.Clear();
        foreach (var cid in dto.CategoryIds.Distinct())
            c.Categories.Add(new CompanyCategory { CompanyId = c.Id, CategoryId = cid });

        await _db.SaveChangesAsync();

        var reloaded = await _db.MaintenanceCompanies
            .Include(x => x.Categories).ThenInclude(cc => cc.Category)
            .FirstAsync(x => x.Id == c.Id);

        return Map(reloaded);
    }

    public async Task<bool> SetActiveStatusAsync(Guid id, bool isActive)
    {
        var c = await _db.MaintenanceCompanies.FindAsync(id);
        if (c == null) return false;

        c.ToggleActiveStatus(isActive); // Using Domain Method
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var c = await _db.MaintenanceCompanies.FindAsync(id);
        if (c == null) return false;

        if (c.ActiveTicketsCount > 0)
            throw new InvalidOperationException("Cannot delete a company with active tickets. Suspend instead.");

        _db.MaintenanceCompanies.Remove(c);
        await _db.SaveChangesAsync();

        // Note: Identity User deletion should be handled by the API Controller or via Domain Events.
        return true;
    }

    public async Task UpdateWorkingHoursAsync(Guid companyId, UpdateWorkingHoursDto dto)
    {
        var existing = await _db.CompanyWorkingHours
            .Where(h => h.CompanyId == companyId).ToListAsync();
        _db.CompanyWorkingHours.RemoveRange(existing);

        foreach (var h in dto.Hours)
        {
            _db.CompanyWorkingHours.Add(new CompanyWorkingHour
            {
                CompanyId = companyId,
                Day = h.Day,
                OpenTime = h.OpenTime,
                CloseTime = h.CloseTime,
                IsClosed = h.IsClosed
            });
        }
        await _db.SaveChangesAsync();
    }

    private static MaintenanceCompanyResponseDto Map(MaintenanceCompany c) =>
        new(
            c.Id, c.CompanyName, c.Email, c.Phone, c.City, c.ServiceRadiusKm,
            c.LogoUrl, c.Description, c.IsActive, c.AverageRating,
            c.CompletedTicketsCount, c.ActiveTicketsCount, c.CreatedAt,
            c.Categories.Select(cc => new CategoryDto(
                cc.Category.Id, cc.Category.Name, cc.Category.Icon,
                cc.Category.Description, cc.Category.IsActive)).ToList()
        );
}