using Domain.Entities;

namespace AI.Domain.Entities
{
    public class BrokerPerformanceScore: BaseEntity
    {
        public Guid BrokerId { get; set; } // 
        public DateOnly PeriodStart { get; set; }
        public DateOnly PeriodEnd { get; set; } // 
        public int ClosedDeals { get; set; } // 
        public decimal AvgRating { get; set; } // 
        public int AvgResponseTimeMinutes { get; set; } // 
        public decimal ConversionRate { get; set; } // 
        public decimal TenantSatisfactionScore { get; set; } // 
        public decimal AiPerformanceScore { get; set; } // 
        public string AiScoreBreakdown { get; set; } = string.Empty; // JSON 
    }
}
