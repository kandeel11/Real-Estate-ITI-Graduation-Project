using Payment.Application.DTOs;
using Payment.Application.Interfaces;

namespace Payment.Infrastructure.Services;

public sealed class MockLeaseService : ILeaseService
{
    public Task<IReadOnlyList<ActiveLeaseDto>> GetActiveLeasesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ActiveLeaseDto> result = new List<ActiveLeaseDto>
        {
            new(
                LeaseId: Guid.Parse("11111111-0000-0000-0000-000000000001"),
                RentAmount: 5000m,
                PaymentDayOfMonth: 1,
                TenantName: "أحمد محمد",
                TenantEmail: "ahmed@example.com",
                TenantPhone: "01012345678"
            ),
            new(
                LeaseId: Guid.Parse("22222222-0000-0000-0000-000000000002"),
                RentAmount: 8000m,
                PaymentDayOfMonth: 5,
                TenantName: "سارة علي",
                TenantEmail: "sara@example.com",
                TenantPhone: "01098765432"
            ),
            new(
                LeaseId: Guid.Parse("33333333-0000-0000-0000-000000000003"),
                RentAmount: 12000m,
                PaymentDayOfMonth: 10,
                TenantName: "محمد إبراهيم",
                TenantEmail: "mohamed@example.com",
                TenantPhone: null
            )
        };

        return Task.FromResult(result);
    }

    public Task<ActiveLeaseDto?> GetLeaseByIdAsync(Guid leaseId, CancellationToken cancellationToken = default)
    {
        var all = new Dictionary<Guid, ActiveLeaseDto>
        {
            [Guid.Parse("11111111-0000-0000-0000-000000000001")] = new(
                Guid.Parse("11111111-0000-0000-0000-000000000001"),
                5000m, 1, "أحمد محمد", "ahmed@example.com", "01012345678"),
            [Guid.Parse("22222222-0000-0000-0000-000000000002")] = new(
                Guid.Parse("22222222-0000-0000-0000-000000000002"),
                8000m, 5, "سارة علي", "sara@example.com", "01098765432"),
            [Guid.Parse("33333333-0000-0000-0000-000000000003")] = new(
                Guid.Parse("33333333-0000-0000-0000-000000000003"),
                12000m, 10, "محمد إبراهيم", "mohamed@example.com", null)
        };

        all.TryGetValue(leaseId, out var lease);
        return Task.FromResult(lease);
    }
}
