using MaintenanceCompanies.Application.Interfaces;
using MediatR;

namespace MaintenanceCompanies.Application.Commands.Categories;

// الـ Command
public record DeleteCategoryCommand(Guid Id) : IRequest<bool>;

// الـ Handler
public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, bool>
{
    private readonly IMaintenanceModuleDbContext _db;

    public DeleteCategoryCommandHandler(IMaintenanceModuleDbContext db) => _db = db;

    public async Task<bool> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _db.MaintenanceCategories.FindAsync(new object[] { request.Id }, cancellationToken);

        if (category == null) return false;

        // Soft Delete via Domain Method
        category.Deactivate();

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}