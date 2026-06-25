using MediatR;

namespace Property.Application.Features.Queries.GetFullyManagedProperties;

public record GetFullyManagedPropertiesQuery : IRequest<List<FullyManagedPropertyDto>>;

public sealed record FullyManagedPropertyDto(
    Guid Id,
    Guid OwnerId,
    Guid? CompanyId,
    Guid? ListingBrokerId,
    string Title,
    string City,
    string Address,
    bool IsFullManagement,
    bool IsMaintenanceEnabled,
    bool IsPaymentEnabled
);
