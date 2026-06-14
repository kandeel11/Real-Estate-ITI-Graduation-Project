using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MaintenanceCompanies.Application.DTOs.Categories;
using MaintenanceCompanies.Application.DTOs.Companies;
using MaintenanceCompanies.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceCompanies.Application.Queries.GetAllCompany
{
    // الـ Query مش بيستقبل أي حاجة وبيرجع لستة من الشركات
    public record GetAllCompaniesQuery() : IRequest<List<MaintenanceCompanyResponseDto>>;

    public class GetAllCompaniesQueryHandler : IRequestHandler<GetAllCompaniesQuery, List<MaintenanceCompanyResponseDto>>
    {
        private readonly IMaintenanceModuleDbContext _db;

        public GetAllCompaniesQueryHandler(IMaintenanceModuleDbContext db) => _db = db;

        public async Task<List<MaintenanceCompanyResponseDto>> Handle(GetAllCompaniesQuery request, CancellationToken cancellationToken)
        {
            return await _db.MaintenanceCompanies
                .Select(c => new MaintenanceCompanyResponseDto(
                    c.Id, c.CompanyName, c.Email, c.Phone, c.City, c.ServiceRadiusKm,
                    c.LogoUrl, c.Description, c.IsActive, c.AverageRating,
                    c.CompletedTicketsCount, c.ActiveTicketsCount, c.CreatedAt,
                    c.Categories.Select(cc => new CategoryDto(
                        cc.Category.Id, cc.Category.Name, cc.Category.Icon,
                        cc.Category.Description, cc.Category.IsActive)).ToList()
                ))
                .ToListAsync(cancellationToken);
        }
    }
}