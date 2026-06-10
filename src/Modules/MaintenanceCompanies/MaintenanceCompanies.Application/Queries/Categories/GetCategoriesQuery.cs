using MaintenanceCompanies.Application.DTOs.Categories;
using MaintenanceCompanies.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceCompanies.Application.Queries.Categories;

// الـ Query
public record GetCategoriesQuery(bool IncludeInactive = false) : IRequest<List<CategoryDto>>;

// الـ Handler
public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, List<CategoryDto>>
{
    private readonly IMaintenanceModuleDbContext _db;

    public GetCategoriesQueryHandler(IMaintenanceModuleDbContext db) => _db = db;

    public async Task<List<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.MaintenanceCategories.AsQueryable();

        if (!request.IncludeInactive)
            query = query.Where(c => c.IsActive);

        return await query.OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Icon, c.Description, c.IsActive))
            .ToListAsync(cancellationToken);
    }
}