using MaintenanceCompanies.Application.DTOs.Categories;
using MaintenanceCompanies.Application.DTOs.Companies;
using MaintenanceCompanies.Application.Interfaces;
using MaintenanceCompanies.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceCompanies.Application.Commands.UpdateCompanyProfile;

public record UpdateCompanyProfileCommand(
    string UserId, // عشان نتأكد إنه بيعدل شركته هو بس
    string CompanyName,
    string Phone,
    string City,
    int ServiceRadiusKm,
    string? Description,
    string? LogoUrl,
    List<Guid> CategoryIds
) : IRequest<MaintenanceCompanyResponseDto>;

public class UpdateCompanyProfileCommandHandler : IRequestHandler<UpdateCompanyProfileCommand, MaintenanceCompanyResponseDto>
{
    private readonly IMaintenanceModuleDbContext _db;

    public UpdateCompanyProfileCommandHandler(IMaintenanceModuleDbContext db) => _db = db;

    public async Task<MaintenanceCompanyResponseDto> Handle(UpdateCompanyProfileCommand request, CancellationToken cancellationToken)
    {
        var c = await _db.MaintenanceCompanies
            .Include(x => x.Categories)
            .FirstOrDefaultAsync(x => x.UserId == request.UserId, cancellationToken);

        if (c == null)
            throw new InvalidOperationException("Company profile not found.");

        // استخدام دالة الدومين اللي عملناها قبل كده
        c.UpdateDetails(
            request.CompanyName,
            request.Phone,
            request.City,
            request.ServiceRadiusKm,
            request.Description,
            request.LogoUrl
        );

        // تحديث الأقسام
        c.Categories.Clear();
        foreach (var cid in request.CategoryIds.Distinct())
            c.Categories.Add(new CompanyCategory { CompanyId = c.Id, CategoryId = cid });

        await _db.SaveChangesAsync(cancellationToken);

        // جلب الداتا بعد التحديث عشان نرجعها
        var reloaded = await _db.MaintenanceCompanies
            .Include(x => x.Categories).ThenInclude(cc => cc.Category)
            .FirstAsync(x => x.Id == c.Id, cancellationToken);

        return new MaintenanceCompanyResponseDto(
            reloaded.Id, reloaded.CompanyName, reloaded.Email, reloaded.Phone, reloaded.City, reloaded.ServiceRadiusKm,
            reloaded.LogoUrl, reloaded.Description, reloaded.IsActive, reloaded.AverageRating,
            reloaded.CompletedTicketsCount, reloaded.ActiveTicketsCount, reloaded.CreatedAt,
            reloaded.Categories.Select(cc => new CategoryDto(
                cc.Category.Id, cc.Category.Name, cc.Category.Icon,
                cc.Category.Description, cc.Category.IsActive)).ToList()
        );
    }
}