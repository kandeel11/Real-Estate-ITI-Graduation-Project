using MediatR;
using LeaseEntity = Document.Domain.Entities.Lease;

namespace Document.Application.Queries.Lease.GetLeaseById;

public record GetLeaseByIdQuery(Guid LeaseId) : IRequest<LeaseDto?>;

public record LeaseDto(
    Guid Id,
    Guid PropertyId,
    Guid TenantId,
    Guid OwnerId,
    decimal? InstallmentAmount,
    string? PaymentFrequency,
    decimal? TotalRentAmount,
    int? DurationInMonths,
    decimal? SecurityDeposit,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int? PaymentDayOfMonth,
    string ExtractionStatus,
    decimal? ExtractionConfidence,
    bool IsActive,
    string ContractFileUrl,
    DateTime CreatedAt,
    // ── Renewal / termination lifecycle ──────────────────────────────────────
    Guid? RenewedFromLeaseId,
    DateTime? RenewalRequestedAt,
    Guid? TerminationRequestedById,
    DateTime? TerminationRequestedAt,
    DateOnly? TerminationEffectiveDate,
    string Status)
{
    /// <summary>
    /// Single source of truth for mapping a Lease entity to its DTO, so every
    /// query (mine / by-owner / by-broker / by-id) exposes the same shape and
    /// the same derived <see cref="Status"/>.
    /// </summary>
    public static LeaseDto FromEntity(LeaseEntity l) => new(
        l.Id, l.PropertyId, l.TenantId, l.OwnerId,
        l.InstallmentAmount, l.PaymentFrequency?.ToString(), l.TotalRentAmount, l.DurationInMonths,
        l.SecurityDeposit, l.StartDate, l.EndDate,
        l.PaymentDayOfMonth, l.ExtractionStatus,
        l.ExtractionConfidence, l.IsActive, l.ContractFileUrl, l.CreatedAt,
        l.RenewedFromLeaseId, l.RenewalRequestedAt,
        l.TerminationRequestedById, l.TerminationRequestedAt, l.TerminationEffectiveDate,
        DeriveStatus(l));

    /// <summary>
    /// Lifecycle status the UI renders. Derived (not stored) so it always reflects
    /// the current flags/dates without a separate column to keep in sync.
    /// </summary>
    public static string DeriveStatus(LeaseEntity l)
    {
        if (l.TerminatedAt is not null) return "terminated";

        if (l.IsActive)
        {
            if (l.TerminationRequestedAt is not null) return "termination_requested";
            if (l.RenewalRequestedAt is not null) return "renewal_requested";
            return "active";
        }

        // Not active: either still awaiting tenant acceptance, declined, or expired.
        if (string.Equals(l.ExtractionStatus, "rejected", StringComparison.OrdinalIgnoreCase))
            return "ended";

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        if (l.EndDate is { } end && end < today) return "ended";

        return "pending";
    }
}
