using MaintenanceCompanies.Application.DTOs.Categories;
using MaintenanceCompanies.Application.DTOs.Companies;
using MaintenanceCompanies.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceCompanies.Application.Queries.GetCompanyProfile;

// الـ Query بيستقبل الـ UserId بس
public record GetCompanyProfileQuery(string UserId) : IRequest<MaintenanceCompanyResponseDto?>;

public class GetCompanyProfileQueryHandler : IRequestHandler<GetCompanyProfileQuery, MaintenanceCompanyResponseDto?>
{
    private readonly IMaintenanceModuleDbContext _db;

    public GetCompanyProfileQueryHandler(IMaintenanceModuleDbContext db) => _db = db;

    public async Task<MaintenanceCompanyResponseDto?> Handle(GetCompanyProfileQuery request, CancellationToken cancellationToken)
    {
        var c = await _db.MaintenanceCompanies
            .Include(x => x.Categories).ThenInclude(cc => cc.Category)
            .FirstOrDefaultAsync(x => x.UserId == request.UserId, cancellationToken);

        if (c == null) return null;

        return new MaintenanceCompanyResponseDto(
            c.Id, c.CompanyName, c.Email, c.Phone, c.City, c.ServiceRadiusKm,
            c.LogoUrl, c.Description, c.IsActive, c.AverageRating,
            c.CompletedTicketsCount, c.ActiveTicketsCount, c.CreatedAt,
            c.Categories.Select(cc => new CategoryDto(
                cc.Category.Id, cc.Category.Name, cc.Category.Icon,
                cc.Category.Description, cc.Category.IsActive)).ToList()
        );
    }
}