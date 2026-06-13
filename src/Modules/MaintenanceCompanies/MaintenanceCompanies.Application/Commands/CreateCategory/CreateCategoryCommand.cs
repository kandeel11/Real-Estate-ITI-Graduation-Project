using MaintenanceCompanies.Application.DTOs.Categories;
using MaintenanceCompanies.Application.Interfaces;
using MaintenanceCompanies.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceCompanies.Application.Commands.CreateCategory;

// 1. الـ Command (البيانات اللي جاية من الـ Request)
public record CreateCategoryCommand(
    string Name,
    string? Icon,
    string? Description
) : IRequest<CategoryDto>;


// 2. الـ Handler (اللوجيك بتاع إنشاء القسم)
public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    private readonly IMaintenanceModuleDbContext _db;

    public CreateCategoryCommandHandler(IMaintenanceModuleDbContext db)
    {
        _db = db;
    }

    public async Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        // التأكد من عدم تكرار اسم القسم (زي: سباكة، كهرباء)
        var exists = await _db.MaintenanceCategories
            .AnyAsync(c => c.Name == request.Name, cancellationToken);

        if (exists)
            throw new InvalidOperationException("Category with this name already exists.");

        // إنشاء القسم الجديد
        var entity = MaintenanceCategory.Create(request.Name, request.Icon, request.Description);

        _db.MaintenanceCategories.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        // إرجاع الـ DTO
        return new CategoryDto(
            entity.Id,
            entity.Name,
            entity.Icon,
            entity.Description,
            entity.IsActive
        );
    }
}