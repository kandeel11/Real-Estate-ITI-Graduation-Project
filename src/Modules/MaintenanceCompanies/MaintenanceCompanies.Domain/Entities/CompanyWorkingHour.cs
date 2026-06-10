namespace MaintenanceCompanies.Domain.Entities
{
    public class CompanyWorkingHour
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CompanyId { get; set; }
        public MaintenanceCompany Company { get; set; } = null!;

        public DayOfWeek Day { get; set; }
        public TimeOnly? OpenTime { get; set; }
        public TimeOnly? CloseTime { get; set; }
        public bool IsClosed { get; set; }
    }
}