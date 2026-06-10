using MaintenanceCompanies.Domain.Enums;

namespace MaintenanceCompanies.Domain.Entities;

public class MaintenanceTicket
{
    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = null!; // ID الساكن
    public Guid? CompanyId { get; private set; }          // الشركة اللي مسكت التيكت
    public string Description { get; private set; } = null!;
    public decimal? FinalPrice { get; private set; }      // السعر بعد المعاينة
    public TicketStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private MaintenanceTicket() { } // For EF Core

    // 1. الشركة توافق تنزل تعاين
    public void AcceptForInspection(Guid companyId)
    {
        if (Status != TicketStatus.Pending)
            throw new InvalidOperationException("Ticket is not pending.");

        CompanyId = companyId;
        Status = TicketStatus.UnderInspection;
        UpdatedAt = DateTime.UtcNow;
    }

    // 2. الشركة تحدد السعر بعد المعاينة
    public void SetPriceAfterInspection(decimal price)
    {
        if (Status != TicketStatus.UnderInspection)
            throw new InvalidOperationException("Ticket is not under inspection.");

        if (price <= 0)
            throw new ArgumentException("Price must be a positive value.");

        FinalPrice = price;
        Status = TicketStatus.AwaitingPayment;
        UpdatedAt = DateTime.UtcNow;
    }

    // 3. الساكن يدفع والتيكت تتقفل
    public void MarkAsPaidAndCompleted()
    {
        if (Status != TicketStatus.AwaitingPayment)
            throw new InvalidOperationException("Ticket is not awaiting payment.");

        Status = TicketStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }
}