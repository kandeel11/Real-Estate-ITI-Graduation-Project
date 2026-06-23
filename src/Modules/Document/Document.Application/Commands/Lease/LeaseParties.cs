using LeaseEntity = Document.Domain.Entities.Lease;

namespace Document.Application.Commands.Lease;

/// <summary>
/// Authorization helpers for the two sides of a lease: the tenant on one side,
/// the landlord (owner, and broker when brokered) on the other. Used by the
/// mutual-consent termination flow so a request and its approval must come from
/// opposite sides.
/// </summary>
public static class LeaseParties
{
    public static bool IsTenant(LeaseEntity lease, Guid userId) => userId == lease.TenantId;

    public static bool IsLandlordSide(LeaseEntity lease, Guid userId) =>
        userId == lease.OwnerId || (lease.BrokerId is { } b && userId == b);

    public static bool IsParty(LeaseEntity lease, Guid userId) =>
        IsTenant(lease, userId) || IsLandlordSide(lease, userId);

    /// <summary>The party (or parties) on the opposite side from <paramref name="userId"/>.</summary>
    public static IEnumerable<Guid> CounterpartiesOf(LeaseEntity lease, Guid userId)
    {
        if (IsTenant(lease, userId))
        {
            yield return lease.OwnerId;
            if (lease.BrokerId is { } b && b != lease.OwnerId) yield return b;
        }
        else
        {
            yield return lease.TenantId;
        }
    }

    /// <summary>True when <paramref name="responderId"/> sits on the side opposite the requester.</summary>
    public static bool IsCounterparty(LeaseEntity lease, Guid requesterId, Guid responderId)
    {
        if (!IsParty(lease, responderId)) return false;
        return IsTenant(lease, requesterId)
            ? IsLandlordSide(lease, responderId)
            : IsTenant(lease, responderId);
    }
}
