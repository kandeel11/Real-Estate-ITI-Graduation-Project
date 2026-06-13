using MaintenanceCompanies.Application.DTOs.Categories;
using MaintenanceCompanies.Application.Interfaces;
using MediatR;

namespace MaintenanceCompanies.Application.Queries.Categories;

// الـ Query
public record GetCategoryByIdQuery(Guid Id) : IRequest<CategoryDto?>;

// الـ Handler
public class GetCategoryByIdQueryHandler : IRequestHandler<GetCategoryByIdQuery, CategoryDto?>
{
    private readonly IMaintenanceModuleDbContext _db;

    public GetCategoryByIdQueryHandler(IMaintenanceModuleDbContext db) => _db = db;

    public async Task<CategoryDto?> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var c = await _db.MaintenanceCategories.FindAsync(new object[] { request.Id }, cancellationToken);

        return c == null ? null : new CategoryDto(c.Id, c.Name, c.Icon, c.Description, c.IsActive);
    }
}