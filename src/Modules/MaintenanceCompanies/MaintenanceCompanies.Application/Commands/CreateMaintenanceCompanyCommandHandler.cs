using MaintenanceCompanies.Application.DTOs.Categories;
using MaintenanceCompanies.Application.DTOs.Companies;
using MaintenanceCompanies.Application.Interfaces;
using MaintenanceCompanies.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceCompanies.Application.Commands.CreateCompany;

public class CreateMaintenanceCompanyCommandHandler : IRequestHandler<CreateMaintenanceCompanyCommand, MaintenanceCompanyResponseDto>
{
    private readonly IMaintenanceModuleDbContext _db;

    public CreateMaintenanceCompanyCommandHandler(IMaintenanceModuleDbContext db)
    {
        _db = db;
    }

    public async Task<MaintenanceCompanyResponseDto> Handle(CreateMaintenanceCompanyCommand request, CancellationToken cancellationToken)
    {
        // 1. التأكد إن الإيميل مش موجود في شركات الصيانة
        var emailNormalized = request.Email.Trim().ToLowerInvariant();
        if (await _db.MaintenanceCompanies.AnyAsync(c => c.Email == emailNormalized, cancellationToken))
            throw new InvalidOperationException("A maintenance company with this email already exists.");

        // 2. التأكد من الأقسام
        var categories = await _db.MaintenanceCategories
            .Where(c => request.CategoryIds.Contains(c.Id) && c.IsActive)
            .ToListAsync(cancellationToken);

        if (categories.Count != request.CategoryIds.Count)
            throw new InvalidOperationException("One or more categories are invalid or inactive.");

        // 3. إنشاء كيان الشركة
        var company = MaintenanceCompany.Create(
            userId: request.UserId,
            adminId: request.AdminId,
            companyName: request.CompanyName,
            serviceTypes: categories.Select(c => c.Name).ToArray(),
            email: request.Email,
            phone: request.Phone,
            city: request.City,
            serviceRadiusKm: request.ServiceRadiusKm,
            description: request.Description
        );

        foreach (var cid in request.CategoryIds)
        {
            company.Categories.Add(new CompanyCategory { CategoryId = cid });
        }

        // 3. ساعات العمل الافتراضية
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
        await _db.SaveChangesAsync(cancellationToken);

        // 4. إرجاع الـ DTO
        var saved = await _db.MaintenanceCompanies
            .Include(x => x.Categories).ThenInclude(cc => cc.Category)
            .FirstAsync(x => x.Id == company.Id, cancellationToken);

        return new MaintenanceCompanyResponseDto(
            saved.Id, saved.CompanyName, saved.Email, saved.Phone, saved.City, saved.ServiceRadiusKm,
            saved.LogoUrl, saved.Description, saved.IsActive, saved.AverageRating,
            saved.CompletedTicketsCount, saved.ActiveTicketsCount, saved.CreatedAt,
            saved.Categories.Select(cc => new CategoryDto(
                cc.Category.Id, cc.Category.Name, cc.Category.Icon,
                cc.Category.Description, cc.Category.IsActive)).ToList()
        );
    }
}