using Payment.Application.Interfaces;

namespace Payment.Infrastructure.Services;

/// <summary>
/// Mock implementation للـ ILeaseService
/// يُستخدم للتطوير والاختبار — في الـ production يُستبدل بـ real implementation
/// من الـ Lease Module
/// 
/// لتفعيله: في PaymentModuleExtensions سجّله مؤقتاً لحد ما زميلك يسلم الـ Lease module
/// </summary>
public sealed class MockLeaseService : ILeaseService
{
    public Task<IReadOnlyList<ActiveLeaseDto>> GetActiveLeasesAsync(CancellationToken cancellationToken = default)
    {
        // بيانات وهمية لـ 3 leases للاختبار
        IReadOnlyList<ActiveLeaseDto> result = new List<ActiveLeaseDto>
        {
            new(
                LeaseId: Guid.Parse("11111111-0000-0000-0000-000000000001"),
                RentAmount: 5000m,
                PaymentDayOfMonth: 1,
                TenantName: "أحمد محمد",
                TenantEmail: "ahmed@example.com",
                TenantPhone: "01012345678",
                PropertyId: Guid.Parse("aaaa1111-0000-0000-0000-000000000001"),
                TenantId: Guid.Parse("bbbb1111-0000-0000-0000-000000000001"),
                LandlordId: Guid.Parse("cccc1111-0000-0000-0000-000000000001")
            ),
            new(
                LeaseId: Guid.Parse("22222222-0000-0000-0000-000000000002"),
                RentAmount: 8000m,
                PaymentDayOfMonth: 5,
                TenantName: "سارة علي",
                TenantEmail: "sara@example.com",
                TenantPhone: "01098765432",
                PropertyId: Guid.Parse("aaaa2222-0000-0000-0000-000000000002"),
                TenantId: Guid.Parse("bbbb2222-0000-0000-0000-000000000002"),
                LandlordId: Guid.Parse("cccc2222-0000-0000-0000-000000000002")
            ),
            new(
                LeaseId: Guid.Parse("33333333-0000-0000-0000-000000000003"),
                RentAmount: 12000m,
                PaymentDayOfMonth: 10,
                TenantName: "محمد إبراهيم",
                TenantEmail: "mohamed@example.com",
                TenantPhone: null,
                PropertyId: Guid.Parse("aaaa3333-0000-0000-0000-000000000003"),
                TenantId: Guid.Parse("bbbb3333-0000-0000-0000-000000000003"),
                LandlordId: Guid.Parse("cccc3333-0000-0000-0000-000000000003")
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
                5000m, 1, "أحمد محمد", "ahmed@example.com", "01012345678",
                Guid.Parse("aaaa1111-0000-0000-0000-000000000001"),
                Guid.Parse("bbbb1111-0000-0000-0000-000000000001"),
                Guid.Parse("cccc1111-0000-0000-0000-000000000001")),
            [Guid.Parse("22222222-0000-0000-0000-000000000002")] = new(
                Guid.Parse("22222222-0000-0000-0000-000000000002"),
                8000m, 5, "سارة علي", "sara@example.com", "01098765432",
                Guid.Parse("aaaa2222-0000-0000-0000-000000000002"),
                Guid.Parse("bbbb2222-0000-0000-0000-000000000002"),
                Guid.Parse("cccc2222-0000-0000-0000-000000000002")),
            [Guid.Parse("33333333-0000-0000-0000-000000000003")] = new(
                Guid.Parse("33333333-0000-0000-0000-000000000003"),
                12000m, 10, "محمد إبراهيم", "mohamed@example.com", null,
                Guid.Parse("aaaa3333-0000-0000-0000-000000000003"),
                Guid.Parse("bbbb3333-0000-0000-0000-000000000003"),
                Guid.Parse("cccc3333-0000-0000-0000-000000000003"))
        };

        all.TryGetValue(leaseId, out var lease);
        return Task.FromResult(lease);
    }
}
