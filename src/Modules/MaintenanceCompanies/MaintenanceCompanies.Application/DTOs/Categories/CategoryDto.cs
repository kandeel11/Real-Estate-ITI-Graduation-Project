namespace MaintenanceCompanies.Application.DTOs.Categories;

public record CategoryDto(Guid Id, string Name, string? Icon, string? Description, bool IsActive);

public record CreateCategoryDto(
    string Name,
    string? Icon,
    string? Description
);

public record UpdateCategoryDto(
    string Name,
    string? Icon,
    string? Description,
    bool IsActive
);