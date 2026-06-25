using MediatR;
using Microsoft.EntityFrameworkCore;
using Property.Application.Interfaces;
using Property.Domain.Enums;

namespace Property.Application.Features.Queries.GetFullyManagedProperties;

public sealed class GetFullyManagedPropertiesHandler
    : IRequestHandler<GetFullyManagedPropertiesQuery, List<FullyManagedPropertyDto>>
{
    private readonly IPropertyRepository _repo;

    public GetFullyManagedPropertiesHandler(IPropertyRepository repo) => _repo = repo;

    public async Task<List<FullyManagedPropertyDto>> Handle(
        GetFullyManagedPropertiesQuery request, CancellationToken ct)
    {
        return await _repo.GetQueryable()
            .Where(p => p.Status == PropertyStatus.Rented && p.IsFullManagement)
            .OrderByDescending(p => p.UpdatedAt)
            .Select(p => new FullyManagedPropertyDto(
                p.Id, p.OwnerId, p.CompanyId, p.ListingBrokerId,
                p.Title, p.City, p.Address,
                p.IsFullManagement, p.IsMaintenanceEnabled, p.IsPaymentEnabled))
            .ToListAsync(ct);
    }
}
