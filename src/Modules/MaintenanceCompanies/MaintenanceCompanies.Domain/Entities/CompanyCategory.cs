namespace MaintenanceCompanies.Domain.Entities
{
    public class CompanyCategory
    {
        public Guid CompanyId { get; set; }
        public MaintenanceCompany Company { get; set; } = null!;

        public Guid CategoryId { get; set; }
        public MaintenanceCategory Category { get; set; } = null!;

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
}