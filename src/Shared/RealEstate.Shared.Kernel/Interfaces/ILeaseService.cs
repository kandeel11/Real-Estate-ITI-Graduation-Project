namespace RealEstate.Shared.Kernel.Interfaces;

public interface ILeaseService
{
    Task<LeaseDto?> GetLeaseAsync(Guid leaseId);
    Task<IEnumerable<LeaseDto>> GetLeasesByTenantAsync(Guid tenantId);
    Task<IEnumerable<LeaseDto>> GetActiveLeasesAsync();
}

public record LeaseDto(
    Guid Id,
    Guid PropertyId,
    Guid TenantId,
    Guid OwnerId,
    decimal? RentAmount,
    int? PaymentDayOfMonth,
    DateOnly? StartDate,
    DateOnly? EndDate,
    bool IsActive);