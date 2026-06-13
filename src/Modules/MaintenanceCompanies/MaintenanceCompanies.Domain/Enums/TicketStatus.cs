namespace MaintenanceCompanies.Domain.Enums;

public enum TicketStatus
{
    Pending = 1,          // تيكت جديدة لسه محدش مسكها
    UnderInspection = 2,  // الشركة قبلتها وبتعمل معاينة
    AwaitingPayment = 3,  // الشركة حددت السعر في انتظار دفع الساكن
    Completed = 4         // تم الدفع وتم الإصلاح (مغلقة)
}