namespace MaintenanceCompanies.Domain.Entities
{
    public class CompanyReview
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CompanyId { get; set; }
        public MaintenanceCompany Company { get; set; } = null!;

        public Guid TicketId { get; set; }                  // ref to Tickets module (no nav prop)
        public string TenantId { get; set; } = null!;

        public int Rating { get; set; }                      // 1-5
        public string? Comment { get; set; }
        public string? CompanyReply { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RepliedAt { get; set; }
    }

}