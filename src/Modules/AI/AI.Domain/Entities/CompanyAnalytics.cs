using Domain.Entities;

namespace AI.Domain.Entities
{
    public class CompanyAnalytics:BaseEntity
    {
        public Guid CompanyId { get; set; } // 
        public DateOnly PeriodStart { get; set; } // 
        public DateOnly PeriodEnd { get; set; } // 
        public int TotalProperties { get; set; } // 
        public int TotalInquiries { get; set; } // 
        public int TotalLeads { get; set; } // 
        public int ClosedDeals { get; set; } // 
        public decimal ConversionRate { get; set; } // 
        public int AvgResponseTimeMinutes { get; set; } // 
        public decimal AvgTenantSatisfaction { get; set; } // 
        public string MostViewedProperties { get; set; } = string.Empty; // JSON 
        public string MostRentedAreas { get; set; } = string.Empty; // JSON 
        public string AiGeneratedInsights { get; set; } = string.Empty; // LLM summary 
    }
}

