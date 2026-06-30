namespace Payment.Domain.Entities;

/// <summary>
/// Snapshot of a lease's payment schedule — stored in the Payment module so it
/// can create subsequent installments without crossing the module boundary to Lease.
/// Saved once when LeaseActivatedIntegrationEvent fires.
/// </summary>
public class LeaseScheduleConfig
{
    public Guid LeaseId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid LandlordId { get; private set; }
    public decimal InstallmentAmount { get; private set; }
    public string PaymentFrequency { get; private set; } = "Monthly";
    public int PaymentDayOfMonth { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }

    private LeaseScheduleConfig() { }

    public static LeaseScheduleConfig Create(
        Guid leaseId,
        Guid propertyId,
        Guid tenantId,
        Guid landlordId,
        decimal installmentAmount,
        string paymentFrequency,
        int paymentDayOfMonth,
        DateOnly startDate,
        DateOnly endDate)
    {
        return new LeaseScheduleConfig
        {
            LeaseId = leaseId,
            PropertyId = propertyId,
            TenantId = tenantId,
            LandlordId = landlordId,
            InstallmentAmount = installmentAmount,
            PaymentFrequency = paymentFrequency,
            PaymentDayOfMonth = paymentDayOfMonth,
            StartDate = startDate,
            EndDate = endDate,
        };
    }

    /// <summary>
    /// Returns the next due date after <paramref name="currentDueDate"/>,
    /// respecting the payment frequency. Returns null if the next date would be past EndDate.
    /// </summary>
    public DateOnly? NextDueDate(DateOnly currentDueDate)
    {
        int monthsToAdd = PaymentFrequency switch
        {
            "Quarterly" => 3,
            "Annual" or "Annually" => 12,
            _ => 1, // Monthly
        };

        var next = currentDueDate.AddMonths(monthsToAdd);
        int day = Math.Min(PaymentDayOfMonth, DateTime.DaysInMonth(next.Year, next.Month));
        next = new DateOnly(next.Year, next.Month, day);

        return next <= EndDate ? next : null;
    }
}
