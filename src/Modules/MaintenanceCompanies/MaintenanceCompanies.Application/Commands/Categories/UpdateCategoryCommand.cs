using MaintenanceCompanies.Application.DTOs.Categories;
using MaintenanceCompanies.Application.Interfaces;
using MediatR;

namespace MaintenanceCompanies.Application.Commands.Categories;

// الـ Command
public record UpdateCategoryCommand(
    Guid Id,
    string Name,
    string? Icon,
    string? Description,
    bool IsActive
) : IRequest<CategoryDto?>;

// الـ Handler
public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, CategoryDto?>
{
    private readonly IMaintenanceModuleDbContext _db;

    public UpdateCategoryCommandHandler(IMaintenanceModuleDbContext db) => _db = db;

    public async Task<CategoryDto?> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _db.MaintenanceCategories.FindAsync(new object[] { request.Id }, cancellationToken);

        if (category == null) return null;

        // استخدام دالة الدومين اللي عملناها للـ Encapsulation
        category.UpdateDetails(request.Name, request.Icon, request.Description, request.IsActive);

        await _db.SaveChangesAsync(cancellationToken);

        return new CategoryDto(
            category.Id,
            category.Name,
            category.Icon,
            category.Description,
            category.IsActive
        );
    }
}