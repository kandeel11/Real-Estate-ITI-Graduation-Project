using MaintenanceCompanies.Application.DTOs.Companies;

namespace MaintenanceCompanies.Application.Interfaces;

public interface IMaintenanceCompanyService
{
    Task<List<MaintenanceCompanyListItemDto>> GetAllAsync(string? city, Guid? categoryId, bool? isActive);
    Task<MaintenanceCompanyResponseDto?> GetByIdAsync(Guid id);
    Task<MaintenanceCompanyResponseDto?> GetByUserIdAsync(string userId);

    // تم إضافة userId هنا ليتطابق مع الـ Service
    Task<MaintenanceCompanyResponseDto> CreateAsync(CreateMaintenanceCompanyDto dto, string userId, string adminId);

    Task<MaintenanceCompanyResponseDto?> UpdateAsync(Guid id, UpdateMaintenanceCompanyDto dto);
    Task<bool> SetActiveStatusAsync(Guid id, bool isActive);
    Task<bool> DeleteAsync(Guid id);
    Task UpdateWorkingHoursAsync(Guid companyId, UpdateWorkingHoursDto dto);
}